using Core.Entities.Request;
using Core.Entities.Response;
using DTO.User;
using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    public class UserController : ApiController
    {
        [HttpGet]
        [Route("api/user/{guid}")]
        public ResGetUser GetUser(Guid guid)
        {
            ResGetUser res = new UserLogic().GetUser(new ReqGetUser { guid = guid });
            if (res.user != null)
            {
                res.user.password = null;
            }

            return res;
        }

        [HttpPost]
        [Route("api/user/register")]
        public ResEnterUser Register(DTOUser dtoUser)
        {
            ReqEnterUser req = new ReqEnterUser();
            req.user = new Core.Entities.Models.User();
            req.user.Name = dtoUser.name;
            req.user.Email = dtoUser.email;
            req.user.LastName = dtoUser.LastName;
            req.user.password = dtoUser.password;
            req.user.Adress = dtoUser.Adress;
            req.user.chargerType = dtoUser.chargerType;
            return new UserLogic().EnterUser(req);
        }

        [HttpPost]
        [Route("api/user/activate")]
        public ResActivateUser Activate(DTOActivateUser dtoActivateUser)
        {
            ReqActivateUser req = new ReqActivateUser();
            req.email = dtoActivateUser.email;
            req.token = dtoActivateUser.token;
            return new UserLogic().ActivateUser(req);
        }

        [HttpPost]
        [Route("api/user/login")]
        public ResLogin Login(DTOLogin dtoLogin)
        {
            ReqLogin req = new ReqLogin();
            req.email = dtoLogin.email;
            req.password = dtoLogin.password;
            return new UserLogic().login(req);
        }

        [HttpPut]
        [Route("api/user/update")]
        public ResUpdateUser Update(DTOUpdateUser dtoUpdateUser)
        {
            ReqUpdateUser req = new ReqUpdateUser();
            req.guid = dtoUpdateUser.guid;
            req.user = new Core.Entities.Models.User();
            req.user.Name = dtoUpdateUser.Name;
            req.user.LastName = dtoUpdateUser.LastName;
            req.user.Adress = dtoUpdateUser.Adress;
            req.user.chargerType = dtoUpdateUser.chargerType;
            return new UserLogic().UpdateUser(req);
        }

        [HttpPost]
        [Route("api/user/logout")]
        public ResLogout Logout(ReqLogout req)
        {
            return new UserLogic().Logout(req);
        }
    }
}