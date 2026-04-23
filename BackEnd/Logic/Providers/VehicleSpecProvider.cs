using Core.Entities.Models;
using Logic.Processors;
using Logic.Services;
using System;
using System.Threading.Tasks;

namespace Logic.Providers
{
    public class VehicleSpecProvider
    {
        private readonly GeminiApiService _apiService = new GeminiApiService();
        private readonly GeminiProcessor _processor = new GeminiProcessor();

        public async Task<VehicleSpecs> GetVehicleSpecs(string brand, string model, int year)
        {
            string prompt = BuildPrompt(brand, model, year);
            string responseJson = await _apiService.SendPrompt(prompt);

            if (string.IsNullOrEmpty(responseJson))
            {
                return null;
            }

            string rawText = _processor.ExtractText(responseJson);

            if (string.IsNullOrEmpty(rawText))
            {
                return null;
            }

            string cleanedJson = _processor.CleanJson(rawText);

            return _processor.ParseVehicleSpecs(cleanedJson);

        }

        private string BuildPrompt(string brand, string model, int year)
        {
            return $@"
            Give me EV vehicle specifications in pure JSON only.
            Car:
            Brand: {brand}
            Model: {model}
            Year: {year}
            Return ONLY this JSON format:
            {{
              ""BatteryCapacityKWh"": number,
              ""VehicleMassKg"": number,
              ""EfficiencyWhPerKm"": number,
              ""RegenEfficiencyPercent"": number
            }}
            Do not include markdown.
            Do not include explanation.
            Only valid JSON.
            ";
        }
    }
}