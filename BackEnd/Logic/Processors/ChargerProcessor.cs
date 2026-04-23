using Core.Entities.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Logic.Processors
{
    public class ChargerProcessor
    {
        public List<ChargerStation> ParseChargers(string jsonResponse)
        {
            var chargers = new List<ChargerStation>();

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                return chargers;
            }

            var json = JArray.Parse(jsonResponse);

            foreach (JObject item in json)
            {
                var addressRaw = item["AddressInfo"] as JObject;


                var address = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
                if (addressRaw != null)
                {
                    foreach (var prop in addressRaw.Properties())
                    {
                        address[prop.Name] = prop.Value;
                    }
                }

                var station = new ChargerStation
                {
                    Id = item["ID"]?.ToString(),
                    Name = address.ContainsKey("Title") ? address["Title"]?.ToString() : null,
                    Latitude = address.ContainsKey("Latitude") ? address["Latitude"]?.ToObject<double?>() ?? 0 : 0,
                    Longitude = address.ContainsKey("Longitude") ? address["Longitude"]?.ToObject<double?>() ?? 0 : 0,
                    Address = address.ContainsKey("AddressLine1") ? address["AddressLine1"]?.ToString() : null,
                    OperationalStatus = item["StatusType"]?["Title"]?.ToString(),
                    UsageType = item["UsageType"]?["Title"]?.ToString(),
                };

                var connections = item["Connections"] as JArray;
                if (connections != null)
                {
                    foreach (var c in connections)
                    {
                        station.Connections.Add(new ChargerConnection
                        {
                            ConnectionType = c["ConnectionType"]?["Title"]?.ToString(),
                            PowerKW = c["PowerKW"]?.ToObject<double?>() ?? 0
                        });
                    }
                }

                // Skip a las estaciones con cordenadas no validas
                if (station.Latitude == 0 && station.Longitude == 0)
                {
                    continue;
                }
                chargers.Add(station);
            }

            return chargers;
        }
    }
}