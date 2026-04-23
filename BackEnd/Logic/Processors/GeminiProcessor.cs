using Core.Entities.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Processors
{
    public class GeminiProcessor
    {
        // Extract Gemini text part
        public string ExtractText(string responseJson)
        {
            // extraemos el texto de la respuesta de Gmini
            dynamic json = JsonConvert.DeserializeObject(responseJson);
            return json?.candidates?[0]?.content?.parts?[0]?.text;
        }

        // Limpia el json 
        public string CleanJson(string raw)
        {
            return raw.Replace("```json", "").Replace("```", "").Trim();
        }

        // Convierte el json limpio en un objeto de VehicleSpecs 
        public VehicleSpecs ParseVehicleSpecs(string cleanedJson)
        {
            return JsonConvert.DeserializeObject<VehicleSpecs>(cleanedJson);
        }
    }
}
