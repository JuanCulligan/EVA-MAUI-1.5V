using System;
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using Core.Entities.Response;
using Core.Entities.Request;
using Core.Entities.Models;
using DataAccess;

namespace Logic
{
    public class ViajeLogic
    {

        public ResSaveRecentLocation SaveRecentLocation(ReqSaveRecentLocation req)
        {
            ResSaveRecentLocation res = new ResSaveRecentLocation();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }
                if (string.IsNullOrEmpty(req.locationName))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    return res;
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    conexion.SP_InsertarHistorial(req.guid,req.locationName,(decimal)req.lat,(decimal)req.lon);
                }

                res.Result = true;
                res.errors = null;
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResGetRecentLocations GetRecentLocations(ReqGetRecentLocations req)
        {
            ResGetRecentLocations res = new ResGetRecentLocations();
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
                    var result = conexion.SP_ObtenerHistorial(req.guid).ToList();

                    res.locations = new List<Location>();

                    foreach (var r in result)
                    {
                        Location location = new Location();
                        location.Id = r.ID_HISTORIAL;
                        location.locationName = r.NOMBRE_UBICACION;
                        location.lat = (double)r.LATITUD;
                        location.lon = (double)r.LONGITUD;
                        res.locations.Add(location);
                    }

                    res.Result = true;
                    res.errors = null;
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResDeleteHistory deleteHistory(ReqDeleteHistory req)
        {
            ResDeleteHistory res = new ResDeleteHistory();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }
                if (String.IsNullOrEmpty(req.idHistorial.ToString()))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    return res;
                }
                else
                {
                    using (ConexionDataContext conexion = new ConexionDataContext())
                    {
                        conexion.SP_EliminarHistorial(req.guid,req.idHistorial);
                    }
                    res.Result = true;
                    res.errors = null;
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }



        public ResSaveFavorite SaveFavorite(ReqSaveFavorite req)
        {
            ResSaveFavorite res = new ResSaveFavorite();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }
                if (string.IsNullOrEmpty(req.locationName))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    return res;
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    conexion.SP_InsertarViajeGuardado(req.guid, req.locationName,(decimal)req.lat,(decimal)req.lon);
                }

                res.Result = true;
                res.errors = null;
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResGetFavorites GetFavorites(ReqGetFavorites req)
        {
            ResGetFavorites res = new ResGetFavorites();
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
                    var result = conexion.SP_ObtenerViajesGuardados(req.guid).ToList();

                    res.favorites = new List<Location>();

                    foreach(var r in result)
                    {
                        Location location = new Location();
                        location.locationName = r.NOMBRE_VIAJE;
                        location.lat = (double)r.LATITUD_DESTINO;
                        location.lon = (double)r.LONGITUD_DESTINO;
                        res.favorites.Add(location);
                    }
                    res.Result = true;
                    res.errors = null;
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResDeleteFavorite DeleteFavorite(ReqDeleteFavorite req)
        {
            ResDeleteFavorite res = new ResDeleteFavorite();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
                }

                if (string.IsNullOrEmpty(req.locationName))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    return res;
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_EliminarViajeGuardado(
                        req.guid,
                        req.locationName,
                        (decimal)req.lat,
                        (decimal)req.lon).FirstOrDefault();

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