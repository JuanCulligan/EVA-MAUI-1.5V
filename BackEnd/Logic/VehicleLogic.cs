using Core.Entities.Models;
using Core.Entities.Request;
using Core.Entities.Response;
using Core.Enums;
using DataAccess;
using Logic.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class VehicleLogic
    {
        public ResGetVehicle GetVehicle(ReqGetVehicle req)
        {
            ResGetVehicle res = new ResGetVehicle();
            res.errors = new List<Error>();
            res.Result = false;
            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_ObtenerVehiculo(req.guid).FirstOrDefault();
                    if (result == null)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    }
                    else
                    {
                        res.Brand = result.MARCA;
                        res.Model = result.MODELO;
                        res.Year = result.ANO;
                        res.Result = true;
                        res.errors = null;
                    }
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            return res;
        }

        public ResUpdateVehicle UpdateVehicle(ReqUpdateVehicle req)
        {
            ResUpdateVehicle res = new ResUpdateVehicle();
            res.errors = new List<Error>();
            res.Result = false;
            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                }
                if (String.IsNullOrEmpty(req.Brand))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }
                if (String.IsNullOrEmpty(req.Model))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }
                if (req.Year <= 0)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }
                if (res.errors.Any())
                {
                    return res;
                }
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_ActualizarVehiculo(req.guid, req.Brand, req.Model, req.Year).FirstOrDefault();
                    if (result == null || result.Resultado == 0)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    }
                    else
                    {
                        res.Result = true;
                        res.errors = null;
                    }
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            return res;
        }

        public ResDeleteVehicle DeleteVehicle(Guid guid)
        {
            ResDeleteVehicle res = new ResDeleteVehicle();
            res.errors = new List<Error>();
            res.Result = false;
            try
            {
                if (guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_EliminarVehiculo(guid).FirstOrDefault();
                    if (result == null || result.Resultado == 0)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    }
                    else
                    {
                        res.Result = true;
                        res.errors = null;
                    }
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            return res;
        }

        public ResSaveVehicle SaveVehicle(ReqSaveVehicle req)
        {
            ResSaveVehicle res = new ResSaveVehicle();
            res.errors = new List<Error>();
            res.Result = false;
            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                }
                if (String.IsNullOrEmpty(req.Brand))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }

                if (String.IsNullOrEmpty(req.Model))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }

                if (req.Year <= 0)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }

                if (res.errors.Any())
                {
                    return res;
                }
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_InsertarVehiculo(
                        req.guid,
                        req.Brand,
                        req.Model,
                        req.Year
                    ).FirstOrDefault();

                    if (result == null || result.Resultado == 0)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    }
                    else
                    {
                        res.Result = true;
                        res.errors = null;
                    }
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            return res;
        }
    }
}
