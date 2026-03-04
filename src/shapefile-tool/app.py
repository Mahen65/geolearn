import io
import os
import tempfile
import zipfile

import geopandas as gpd
import psycopg2
import psycopg2.extras
from flask import Flask, jsonify, render_template, request, send_file
from shapely.geometry import shape

app = Flask(__name__)

# Resolve source shapefile relative to this file
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
SOURCE_ZIP = os.environ.get(
    "SOURCE_SHAPEFILE",
    os.path.join(BASE_DIR, "..", "..", "hotosm_lka_buildings_polygons_shp.zip"),
)

DATABASE_URL = os.environ.get(
    "DATABASE_URL",
    "postgresql://geolearn:geolearn@localhost:5432/geolearn",
)


def _get_conn():
    return psycopg2.connect(DATABASE_URL)


@app.route("/")
def index():
    return render_template("index.html")


@app.route("/countries")
def countries():
    conn = _get_conn()
    try:
        with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
            cur.execute("SELECT name, lat, lng, zoom FROM countries ORDER BY name")
            rows = cur.fetchall()
        return jsonify([dict(r) for r in rows])
    finally:
        conn.close()


@app.route("/clip", methods=["POST"])
def clip():
    """Clip the source shapefile to the drawn polygon and return a zip."""
    data = request.get_json(force=True)
    geojson_geom = data.get("geometry")
    if not geojson_geom:
        return jsonify({"error": "No geometry provided"}), 400

    clip_poly = shape(geojson_geom)
    bbox = clip_poly.bounds  # (minx, miny, maxx, maxy) — passed to GDAL for fast spatial filter

    if not os.path.exists(SOURCE_ZIP):
        return jsonify({"error": f"Source shapefile not found: {SOURCE_ZIP}"}), 500

    try:
        # bbox param makes GDAL read only features within bounds — avoids loading all 3.7M rows
        source_gdf = gpd.read_file(SOURCE_ZIP, bbox=bbox)
    except Exception as e:
        return jsonify({"error": f"Failed to read source shapefile: {e}"}), 500

    if source_gdf.empty:
        return jsonify({"error": "No features found in the selected area"}), 404

    # Precise clip (bbox is a rectangle; clip trims to the actual drawn shape)
    clip_gdf = gpd.GeoDataFrame([{"geometry": clip_poly}], crs="EPSG:4326")
    if source_gdf.crs and source_gdf.crs != clip_gdf.crs:
        clip_gdf = clip_gdf.to_crs(source_gdf.crs)

    clipped = gpd.clip(source_gdf, clip_gdf)
    if clipped.empty:
        return jsonify({"error": "No features in the selected area after clipping"}), 404

    return _shapefile_zip_response(clipped, "clipped_buildings")


@app.route("/export-polygon", methods=["POST"])
def export_polygon():
    """Export the drawn polygon itself as a shapefile (no source data needed)."""
    data = request.get_json(force=True)
    geojson_geom = data.get("geometry")
    if not geojson_geom:
        return jsonify({"error": "No geometry provided"}), 400

    poly = shape(geojson_geom)
    gdf = gpd.GeoDataFrame([{"geometry": poly, "name": "selected_area"}], crs="EPSG:4326")
    return _shapefile_zip_response(gdf, "selected_area")


@app.route("/feature-count", methods=["POST"])
def feature_count():
    """Return how many features are in the drawn area (quick check before clipping)."""
    data = request.get_json(force=True)
    geojson_geom = data.get("geometry")
    if not geojson_geom:
        return jsonify({"error": "No geometry provided"}), 400

    clip_poly = shape(geojson_geom)
    bbox = clip_poly.bounds

    if not os.path.exists(SOURCE_ZIP):
        return jsonify({"error": "Source shapefile not found"}), 500

    try:
        gdf = gpd.read_file(SOURCE_ZIP, bbox=bbox)
        return jsonify({"count": len(gdf)})
    except Exception as e:
        return jsonify({"error": str(e)}), 500


def _shapefile_zip_response(gdf: gpd.GeoDataFrame, name: str):
    """Write gdf to a shapefile bundle, zip it, return as a download."""
    with tempfile.TemporaryDirectory() as tmpdir:
        shp_path = os.path.join(tmpdir, f"{name}.shp")
        gdf.to_file(shp_path)

        zip_buffer = io.BytesIO()
        with zipfile.ZipFile(zip_buffer, "w", zipfile.ZIP_DEFLATED) as zf:
            for fname in os.listdir(tmpdir):
                zf.write(os.path.join(tmpdir, fname), fname)
        zip_buffer.seek(0)

    return send_file(
        zip_buffer,
        mimetype="application/zip",
        as_attachment=True,
        download_name=f"{name}.zip",
    )


if __name__ == "__main__":
    debug = os.environ.get("FLASK_DEBUG", "1") == "1"
    app.run(host="0.0.0.0", port=5001, debug=debug)
