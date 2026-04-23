using Core.Entities.Models;
using Core.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Logic.Algorithm
{
    public class AlgBatteryEstimation
    {
        public BatteryEstimationResponse BateryEstimation(List<RouteSegment> segments, VehicleSpecs vehicle, DrivingConditions conditions)
        {
            double totalEnergyWh = 0;

            foreach (var segment in segments)
            {
                double distanceKm = segment.DistanceMeters / 1000.0; // pasar metros a kilometros
                double energyFlat = distanceKm * vehicle.EfficiencyWhPerKm; // calcular el consumo en plano
                totalEnergyWh += energyFlat;


                if (segment.ElevationChange > 0)
                {
                    //calculamos el consumo en subida y pasamo de Joules a Wh
                    double uphillConsumption = (vehicle.VehicleMassKg * 9.81 * segment.ElevationChange) / 3600.0;
                    totalEnergyWh += uphillConsumption;
                }
                else if (segment.ElevationChange < 0)
                {
                    // calcula el la bateria regenerada en bajadas 
                    double downhillWh = (vehicle.VehicleMassKg * 9.81 * Math.Abs(segment.ElevationChange)) / 3600.0;
                    double regenFactor = vehicle.RegenEfficiencyPercent / 100.0;
                    double recoveredWh = downhillWh * regenFactor;

                    totalEnergyWh -= recoveredWh;

                }
            }


            //Global 
            double speedFactor = 1.0;
            double tempFactor = 1.0;

            if (conditions.SpeedKmh > 80)
            {
                speedFactor += (conditions.SpeedKmh - 80) * 0.01;
            }
            if (conditions.TemperatureC < 10)
            {
                tempFactor += (10 - conditions.TemperatureC) * 0.02;
            }
            speedFactor = Math.Min(speedFactor, 1.5);
            tempFactor = Math.Min(tempFactor, 1.5);
            // Aplica penalización de velocidad y temperatura 
            totalEnergyWh *= speedFactor * tempFactor;


            // penalización por AC
            if (conditions.TemperatureC < 10)
            {
                totalEnergyWh += 500; // extra Wh for cabin heating
            }
            // penalización por detenerse y despues seguir 
            if (conditions.SpeedKmh < 30)
            {
                totalEnergyWh *= 1.15; // +15% more consumption
            }

            if (totalEnergyWh < 0)
            {
                totalEnergyWh = 0;
            }

            // calcula el % de bateria usada
            double batteryWh = vehicle.BatteryCapacityKWh * 1000.0;
            double percentUsed = (totalEnergyWh / batteryWh) * 100.0;

            return new BatteryEstimationResponse { 
                TotalEnergyWh = totalEnergyWh,
                PercentUsed = percentUsed,
            };
        }




    }
}
