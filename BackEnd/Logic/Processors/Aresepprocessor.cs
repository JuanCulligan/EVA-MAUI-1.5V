using Newtonsoft.Json.Linq;
using System;

namespace Logic.Processors
{
    public class AresepProcessor
    {
        public double GetCostoColones(string jsonResponse, double totalEnergyWh)
        {
            double consumoKwh = totalEnergyWh / 1000.0;
            double tarifaColones = ObtenerUltimaTarifaICE(jsonResponse);

            if (tarifaColones == 0)
                return 0;

            return Math.Round(consumoKwh * tarifaColones, 2);
        }

        private double ObtenerUltimaTarifaICE(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return 0;

            try
            {
                var root = JObject.Parse(jsonResponse);
                var items = root["value"] as JArray;

                if (items == null)
                    return 0;

                int mejorAnho = 0;
                int mejorMes = 0;
                double mejorPrecio = 0;

                foreach (JObject item in items)
                {
                    string empresa = item["empresa"]?.ToString() ?? string.Empty;
                    string tipoTarifa = item["tipoTarifa"]?.ToString() ?? string.Empty;

                    bool esICE = empresa.IndexOf("ICE", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool esResidencial = tipoTarifa.IndexOf("Residencial", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!esICE || !esResidencial)
                        continue;

                    int anho = item["anho"]?.ToObject<int>() ?? 0;
                    int idMes = item["id_Mes"]?.ToObject<int>() ?? 0;
                    double precio = item["precioMedioConCVG"]?.ToObject<double>() ?? 0;

                    if (precio <= 0)
                        continue;

                    if (anho > mejorAnho || (anho == mejorAnho && idMes > mejorMes))
                    {
                        mejorAnho = anho;
                        mejorMes = idMes;
                        mejorPrecio = precio;
                    }
                }

                return mejorPrecio;
            }
            catch
            {
                return 0;
            }
        }
    }
}