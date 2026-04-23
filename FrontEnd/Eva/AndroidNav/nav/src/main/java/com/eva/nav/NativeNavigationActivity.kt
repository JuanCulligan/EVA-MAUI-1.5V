package com.eva.nav

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Color
import android.os.Bundle
import android.util.Log
import android.view.Gravity
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.core.view.WindowCompat
import androidx.startup.AppInitializer
import com.mapbox.common.MapboxOptions
import com.mapbox.common.MapboxSDKCommonInitializer
import com.mapbox.geojson.LineString
import com.mapbox.geojson.Point
import com.mapbox.maps.CameraOptions
import com.mapbox.maps.EdgeInsets
import com.mapbox.maps.MapView
import com.mapbox.maps.Style
import com.mapbox.maps.extension.style.layers.generated.lineLayer
import com.mapbox.maps.extension.style.sources.generated.geoJsonSource
import com.mapbox.maps.loader.MapboxMapsInitializer
import com.mapbox.maps.plugin.locationcomponent.createDefault2DPuck
import com.mapbox.maps.plugin.locationcomponent.location
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.util.concurrent.Executors
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.roundToInt
import kotlin.math.sin

/**
 * Viaje nativo: misma ruta que el mapa web (Directions API), línea en mapa, cámara inclinada,
 * puck de ubicación si hay permiso, panel inferior tipo “modo viaje”.
 *
 * Nota: no es Mapbox Navigation SDK (sin voces/maniobras paso a paso nativas); es Maps + ruta + UI.
 */
class NativeNavigationActivity : AppCompatActivity() {

    private var mapView: MapView? = null
    private var navPanel: LinearLayout? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        WindowCompat.setDecorFitsSystemWindows(window, false)

        val token = intent?.getStringExtra("mapbox_access_token")?.trim().orEmpty()
        if (token.isBlank()) {
            Toast.makeText(this, "Error: falta token de Mapbox (pk.) para mapa nativo", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        val olng = intent.getStringExtra("olng")?.trim()?.toDoubleOrNull()
        val olat = intent.getStringExtra("olat")?.trim()?.toDoubleOrNull()
        val dlng = intent.getStringExtra("dlng")?.trim()?.toDoubleOrNull()
        val dlat = intent.getStringExtra("dlat")?.trim()?.toDoubleOrNull()
        val destName = intent.getStringExtra("name")?.trim().orEmpty()

        Log.i(
            "EvaNav",
            "Intent trip olng=$olng olat=$olat dlng=$dlng dlat=$dlat name=${destName.take(40)}"
        )

        try {
            val ai = AppInitializer.getInstance(applicationContext)
            ai.initializeComponent(MapboxSDKCommonInitializer::class.java)
            ai.initializeComponent(MapboxMapsInitializer::class.java)
        } catch (t: Throwable) {
            Log.e("EvaNav", "Mapbox AppInitializer", t)
            Toast.makeText(this, "Error iniciando Mapbox: ${t.javaClass.simpleName} — ${t.message ?: t.cause?.message}", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        try {
            MapboxOptions.accessToken = token
        } catch (t: Throwable) {
            Log.e("EvaNav", "MapboxOptions.accessToken", t)
            Toast.makeText(this, "Error MapboxOptions: ${t.javaClass.simpleName} — ${t.message ?: t.cause?.message}", Toast.LENGTH_LONG).show()
            finish()
            return
        }

        val root = FrameLayout(this).apply {
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
        }
        setContentView(root)

        val caption = TextView(this).apply {
            text = if (destName.isNotEmpty()) destName else "Viaje"
            setTextColor(0xE6FFFFFF.toInt())
            textSize = 16f
            setPadding(24, 48, 24, 12)
            setShadowLayer(4f, 0f, 1f, 0xFF000000.toInt())
        }
        root.addView(
            caption,
            FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.WRAP_CONTENT
            )
        )

        val panel = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(20, 16, 20, 28)
            setBackgroundColor(0xE6161820.toInt())
        }
        val titleNav = TextView(this).apply {
            text = "Modo viaje"
            setTextColor(Color.WHITE)
            textSize = 18f
        }
        val subNav = TextView(this).apply {
            text = "Cargando ruta…"
            setTextColor(0xFFB0B8C8.toInt())
            textSize = 14f
        }
        panel.addView(titleNav)
        panel.addView(subNav)
        navPanel = panel
        val panelLp = FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.MATCH_PARENT,
            FrameLayout.LayoutParams.WRAP_CONTENT
        ).apply { gravity = Gravity.BOTTOM }
        root.addView(panel, panelLp)

        try {
            val mv = MapView(this).apply {
                layoutParams = FrameLayout.LayoutParams(
                    FrameLayout.LayoutParams.MATCH_PARENT,
                    FrameLayout.LayoutParams.MATCH_PARENT
                )
            }
            mapView = mv
            root.addView(mv, 0)

            val hasTrip = olng != null && olat != null && dlng != null && dlat != null
            if (!hasTrip) {
                Log.w("EvaNav", "Sin coordenadas completas en el intent")
                subNav.text = "Sin origen/destino: vuelve al mapa e inicia el viaje de nuevo."
                Toast.makeText(this, "Faltan coordenadas del viaje", Toast.LENGTH_LONG).show()
                mv.mapboxMap.loadStyle(Style.DARK) { enableLocationIfAllowed(mv) }
            } else {
                mv.mapboxMap.loadStyle(Style.DARK) { _ ->
                    enableLocationIfAllowed(mv)
                    fetchRouteAndDraw(
                        mv,
                        token,
                        olng,
                        olat,
                        dlng,
                        dlat,
                        destName,
                        subNav
                    )
                }
            }
        } catch (t: Throwable) {
            Toast.makeText(this, "Error creando MapView: ${t.message}", Toast.LENGTH_LONG).show()
            finish()
        }
    }

    private fun enableLocationIfAllowed(mv: MapView) {
        val fine = Manifest.permission.ACCESS_FINE_LOCATION
        if (ContextCompat.checkSelfPermission(this, fine) == PackageManager.PERMISSION_GRANTED) {
            try {
                mv.location.updateSettings {
                    locationPuck = createDefault2DPuck(withBearing = true)
                    enabled = true
                }
            } catch (t: Throwable) {
                Log.w("EvaNav", "Location puck", t)
            }
        } else {
            ActivityCompat.requestPermissions(this, arrayOf(fine), REQ_LOC)
        }
    }

    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == REQ_LOC && grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
            mapView?.let { enableLocationIfAllowed(it) }
        }
    }

    private fun fetchRouteAndDraw(
        mv: MapView,
        token: String,
        olng: Double,
        olat: Double,
        dlng: Double,
        dlat: Double,
        destName: String,
        subNav: TextView
    ) {
        val exec = Executors.newSingleThreadExecutor()
        exec.execute {
            var line: LineString? = null
            var distanceM: Double? = null
            var durationSec: Double? = null
            var httpErr: String? = null
            try {
                val path = "$olng,$olat;$dlng,$dlat"
                val u = URL(
                    "https://api.mapbox.com/directions/v5/mapbox/driving/" +
                        path +
                        "?access_token=" + java.net.URLEncoder.encode(token, Charsets.UTF_8.name()) +
                        "&geometries=geojson&overview=full&steps=false&language=es"
                )
                val conn = (u.openConnection() as HttpURLConnection).apply {
                    requestMethod = "GET"
                    connectTimeout = 25000
                    readTimeout = 25000
                }
                val code = conn.responseCode
                val body = (if (code in 200..299) conn.inputStream else conn.errorStream)
                    ?.bufferedReader(Charsets.UTF_8)
                    ?.use { it.readText() }
                    .orEmpty()
                if (code !in 200..299) {
                    httpErr = "HTTP $code ${body.take(200)}"
                    Log.e("EvaNav", "Directions: $httpErr")
                    exec.shutdown()
                    runOnUiThread {
                        subNav.text = "No se pudo calcular la ruta (revisa red y token)."
                        Toast.makeText(this, "Directions: código $code", Toast.LENGTH_LONG).show()
                    }
                    return@execute
                }
                val root = JSONObject(body)
                val routes = root.optJSONArray("routes")
                if (routes == null || routes.length() == 0) {
                    httpErr = root.optString("message", "sin rutas")
                    Log.e("EvaNav", "Directions sin routes: $httpErr")
                    exec.shutdown()
                    runOnUiThread {
                        subNav.text = "Sin ruta entre origen y destino."
                    }
                    return@execute
                }
                val rt = routes.getJSONObject(0)
                distanceM = rt.optDouble("distance", Double.NaN).takeIf { !it.isNaN() }
                durationSec = rt.optDouble("duration", Double.NaN).takeIf { !it.isNaN() }
                val geom = rt.optJSONObject("geometry") ?: return@execute
                val coords = geom.optJSONArray("coordinates") ?: return@execute
                val pts = ArrayList<Point>(coords.length())
                for (i in 0 until coords.length()) {
                    val c = coords.getJSONArray(i)
                    pts.add(Point.fromLngLat(c.getDouble(0), c.getDouble(1)))
                }
                if (pts.size >= 2) {
                    line = LineString.fromLngLats(pts)
                }
            } catch (t: Throwable) {
                Log.e("EvaNav", "Directions API", t)
                httpErr = t.message
            } finally {
                exec.shutdown()
            }

            val finalLine = line
            runOnUiThread {
                if (isDestroyed) return@runOnUiThread
                if (finalLine == null) {
                    subNav.text = httpErr?.let { "Error: $it" } ?: "No se pudo dibujar la ruta."
                    Toast.makeText(this, subNav.text.toString(), Toast.LENGTH_LONG).show()
                    return@runOnUiThread
                }

                val distKm = distanceM?.let { (it / 1000.0 * 10).roundToInt() / 10.0 }
                val minEta = durationSec?.let { (it / 60).roundToInt() }
                subNav.text = buildString {
                    if (destName.isNotEmpty()) append(destName).append(" · ")
                    if (distKm != null) append("~${distKm} km")
                    if (minEta != null) {
                        if (isNotEmpty() && !endsWith(" · ")) append(" · ")
                        append("~${minEta} min")
                    }
                }.ifEmpty { "Sigue la línea azul hacia el destino." }

                mv.post {
                    if (isDestroyed) return@post
                    mv.mapboxMap.getStyle { style ->
                        if (style == null) {
                            Log.e("EvaNav", "getStyle devolvió null")
                            return@getStyle
                        }
                        try {
                            if (style.styleSourceExists("eva-trip-route")) {
                                style.removeStyleLayer("eva-trip-route-line")
                                style.removeStyleSource("eva-trip-route")
                            }
                            style.addSource(
                                geoJsonSource("eva-trip-route") {
                                    geometry(finalLine)
                                }
                            )
                            style.addLayer(
                                lineLayer("eva-trip-route-line", "eva-trip-route") {
                                    lineColor("#3b82f6")
                                    lineWidth(6.0)
                                    lineOpacity(0.92)
                                }
                            )
                        } catch (t: Throwable) {
                            Log.e("EvaNav", "addSource/layer", t)
                            Toast.makeText(this, "Error capa ruta: ${t.message}", Toast.LENGTH_LONG).show()
                            return@getStyle
                        }

                        val pts = finalLine.coordinates() ?: emptyList()
                        if (pts.size < 2) return@getStyle

                        val bearing = bearingDegrees(
                            pts[0].longitude(), pts[0].latitude(),
                            pts[minOf(pts.size - 1, 32)].longitude(),
                            pts[minOf(pts.size - 1, 32)].latitude()
                        )
                        val pad = EdgeInsets(160.0, 48.0, 260.0, 48.0)
                        val camSeed = CameraOptions.Builder()
                            .bearing(bearing)
                            .pitch(52.0)
                            .build()

                        mv.mapboxMap.cameraForCoordinates(
                            pts,
                            camSeed,
                            pad,
                            16.5,
                            null
                        ) { opts ->
                            if (opts.center != null) {
                                mv.mapboxMap.setCamera(opts)
                            } else {
                                mv.mapboxMap.setCamera(
                                    CameraOptions.Builder()
                                        .center(pts[pts.size / 2])
                                        .zoom(12.5)
                                        .bearing(bearing)
                                        .pitch(48.0)
                                        .build()
                                )
                            }
                        }
                        Log.i("EvaNav", "Ruta aplicada: ${pts.size} pts, dist=${distanceM}m")
                    }
                }
            }
        }
    }

    private fun bearingDegrees(lon1: Double, lat1: Double, lon2: Double, lat2: Double): Double {
        val phi1 = Math.toRadians(lat1)
        val phi2 = Math.toRadians(lat2)
        val dLon = Math.toRadians(lon2 - lon1)
        val y = sin(dLon) * cos(phi2)
        val x = cos(phi1) * sin(phi2) - sin(phi1) * cos(phi2) * cos(dLon)
        return (Math.toDegrees(atan2(y, x)) + 360.0) % 360.0
    }

    override fun onStart() {
        super.onStart()
        mapView?.onStart()
    }

    override fun onStop() {
        super.onStop()
        mapView?.onStop()
    }

    override fun onDestroy() {
        mapView?.onDestroy()
        mapView = null
        navPanel = null
        super.onDestroy()
    }

    override fun onLowMemory() {
        super.onLowMemory()
        mapView?.onLowMemory()
    }

    companion object {
        private const val REQ_LOC = 4401
    }
}
