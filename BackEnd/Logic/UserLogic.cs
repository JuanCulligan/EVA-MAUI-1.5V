using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Enums;
using Core.Entities.Response;
using Core.Entities.Request;
using Core.Entities;
using Helpers;
using Core.Entities.Models;
using System.Reflection;
using Newtonsoft.Json;
using DataAccess;

namespace Logic
{
    public class UserLogic
    {
        public ResEnterUser EnterUser(ReqEnterUser req)
        {
            ResEnterUser res = new ResEnterUser();
            res.Result = false;
            res.errors = new List<Error>();
            EnumLog enumLog = EnumLog.Fail;
            int errorID = 0;
            string errorDescription = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(req.user.Name))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingName));
                    errorID = (int)EnumErrors.missingName;
                    errorDescription = EnumErrors.missingName.ToString();
                }
                if (String.IsNullOrEmpty(req.user.LastName))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingLastName));
                    errorID = (int)EnumErrors.missingLastName;
                    errorDescription = EnumErrors.missingLastName.ToString();
                }
                if (String.IsNullOrEmpty(req.user.Email))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingEmail));
                    errorID = (int)EnumErrors.missingEmail;
                    errorDescription = EnumErrors.missingEmail.ToString();
                }
                else if (!Helpers.Helpers.IsValidEmail(req.user.Email))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.InvalidEmail));
                }
                if (String.IsNullOrEmpty(req.user.password))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingPassword));
                    errorID = (int)EnumErrors.missingPassword;
                    errorDescription = EnumErrors.missingPassword.ToString();
                }
                else if (!Helpers.Helpers.IsStrongPassword(req.user.password))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.PasswordNotStrong));
                }
                if (String.IsNullOrEmpty(req.user.Adress))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingAdress));
                    errorID = (int)EnumErrors.missingAdress;
                    errorDescription = EnumErrors.missingAdress.ToString();
                }

                if (res.errors.Any())
                {
                    return res;
                }
                else
                {
                    req.user.password = Helpers.Helpers.HashPassword(req.user.password);

                    Guid? guid = null;
                    string token = Helpers.Helpers.createToken();

                    using (ConexionDataContext conexionDataContext = new ConexionDataContext())
                    {
                        var retunedData = conexionDataContext.SP_InsertarUsuario(
                            req.user.Name,
                            req.user.LastName,
                            req.user.Email,
                            req.user.password,
                            req.user.Adress,
                            req.user.chargerType,
                            token
                            ).FirstOrDefault();

                        if (retunedData == null || retunedData.Resultado == 0)
                        {
                            res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.EmailAlreadyExists));
                            return res;
                        }
                        guid = retunedData.GUID_USUARIO;
                    }

                    if (guid == null)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                    }
                    else
                    {
                        if (Helpers.Helpers.SendVerificationEmail(req.user.Name, req.user.Email, token))
                        {
                            res.Result = true;
                            res.errors = null;
                        }
                        else
                        {
                            using (ConexionDataContext conexionDataContext = new ConexionDataContext())
                            {
                                conexionDataContext.SP_EliminarUsuario(guid);
                            }
                            res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            finally
            {
                ReqLogs reqLogs = new ReqLogs();
                Log log = new Log();

                log.Class = GetType().Name;
                log.Method = MethodBase.GetCurrentMethod().Name;

                if (res.Result == true) { log.type = EnumLog.success; } else { log.type = EnumLog.Fail; }

                log.ErrorID = errorID;
                log.Description = errorDescription;
                log.Request = JsonConvert.SerializeObject(req);
                log.Response = JsonConvert.SerializeObject(res);

                reqLogs.log = log;
                Helpers.Helpers.LogActions(reqLogs);
            }
            return res;
        }

        public ResGetUser GetUser(ReqGetUser req)
        {
            ResGetUser res = new ResGetUser();
            res.errors = new List<Error>();
            res.Result = false;
            try
            {
                if (req.guid.Equals(Guid.Empty))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                }
                if (res.errors.Any())
                {
                    return res;
                }
                else
                {
                    SP_ObtenerUsuarioPorGuidResult sP_Obtener = null;
                    using (ConexionDataContext conexion = new ConexionDataContext())
                    {
                        sP_Obtener = conexion.SP_ObtenerUsuarioPorGuid(req.guid).ToList().FirstOrDefault();
                    }

                    if (sP_Obtener == null)
                    {
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                        return res;
                    }

                    res.user = this.factoriaUser(sP_Obtener);
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

        public ResLogin login(ReqLogin req)
        {
            ResLogin res = new ResLogin();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (String.IsNullOrEmpty(req.email))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingName));
                }
                else if (!Helpers.Helpers.IsValidEmail(req.email))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.InvalidEmail));
                }
                if (String.IsNullOrEmpty(req.password))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingPassword));
                }

                if (res.errors.Any())
                {
                    return res;
                }

                SP_LoginUsuarioResult sP_LoginUsuario = new SP_LoginUsuarioResult();
                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    sP_LoginUsuario = conexion.SP_LoginUsuario(req.email).ToList().FirstOrDefault();
                }

                if (sP_LoginUsuario == null)
                {
                    res.Result = false;
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.IncorrectEmail));
                    return res;
                }

                if (!Helpers.Helpers.VerifyPassword(req.password, sP_LoginUsuario.PASSWORD))
                {
                    res.Result = false;
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.wrongPassword));
                    return res;
                }

                res.Result = true;
                res.user = new Core.Entities.Models.User();
                res.user.guid = sP_LoginUsuario.GUID_USUARIO;
                res.user.Name = sP_LoginUsuario.NOMBRE;
                res.user.LastName = sP_LoginUsuario.APELLIDOS;
                res.user.Email = sP_LoginUsuario.CORREO_ELECTRONICO;
                res.user.Adress = sP_LoginUsuario.DIRECCION ?? string.Empty;
                res.user.chargerType = sP_LoginUsuario.TIPO_DE_CARGADOR ?? string.Empty;
                return res;
            }
            catch (Exception ex)
            {
                res.Result = false;
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }
            return res;
        }

        public ResActivateUser ActivateUser(ReqActivateUser req)
        {
            ResActivateUser res = new ResActivateUser();
            res.errors = new List<Error>();

            try
            {
                if (String.IsNullOrEmpty(req.email))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingEmail));
                }
                if (String.IsNullOrEmpty(req.token))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ErrorActivatingUser));
                }

                if (res.errors.Any())
                {
                    res.Result = false;
                    return res;
                }

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var resultado = conexion.SP_ActivarUsuario(req.email, req.token).FirstOrDefault();

                    if (resultado == null || resultado.Resultado == 0)
                    {
                        res.Result = false;
                        res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ErrorActivatingUser));
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
                res.Result = false;
                res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.ExceptionThrown));
            }

            return res;
        }

        public ResUpdateUser UpdateUser(ReqUpdateUser req)
        {
            ResUpdateUser res = new ResUpdateUser();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                }
                if (String.IsNullOrEmpty(req.user.Name))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingName));
                }
                if (String.IsNullOrEmpty(req.user.LastName))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingLastName));
                }
                if (String.IsNullOrEmpty(req.user.Adress))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.missingAdress));
                }
                if (String.IsNullOrEmpty(req.user.chargerType))
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.DataBaseError));
                }

                if (res.errors.Any()) return res;

                using (ConexionDataContext conexion = new ConexionDataContext())
                {
                    var result = conexion.SP_ActualizarUsuario(
                        req.guid,
                        req.user.Name,
                        req.user.LastName,
                        req.user.Adress,
                        req.user.chargerType
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

        public ResLogout Logout(ReqLogout req)
        {
            ResLogout res = new ResLogout();
            res.errors = new List<Error>();
            res.Result = false;

            try
            {
                if (req.guid == Guid.Empty)
                {
                    res.errors.Add(Helpers.Helpers.CreateError(EnumErrors.GuidMissing));
                    return res;
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

        private Core.Entities.Models.User factoriaUser(SP_ObtenerUsuarioPorGuidResult complexType)
        {
            Core.Entities.Models.User user = new Core.Entities.Models.User();
            user.guid = complexType.GUID_USUARIO;
            user.Name = complexType.NOMBRE;
            user.LastName = complexType.APELLIDOS;
            user.Email = complexType.CORREO_ELECTRONICO;
            user.Adress = complexType.DIRECCION;
            user.password = complexType.PASSWORD;
            user.chargerType = complexType.TIPO_DE_CARGADOR;
            return user;
        }
    }
}