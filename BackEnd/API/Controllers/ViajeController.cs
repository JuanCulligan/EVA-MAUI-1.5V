using Core.Entities.Request;
using Core.Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace API.Controllers
{
    public class ViajeController : ApiController
    {
        [HttpPost]
        [Route("api/viaje/saveRecentLocation")]
        public ResSaveRecentLocation SaveRecentLocation(ReqSaveRecentLocation req)
        {
            return new ViajeLogic().SaveRecentLocation(req);
        }

        [HttpGet]
        [Route("api/viaje/getRecentLocations/{guid}")]
        public ResGetRecentLocations GetRecentLocations(Guid guid)
        {
            return new ViajeLogic().GetRecentLocations(new ReqGetRecentLocations { guid = guid });
        }

        [HttpPost]
        [Route("api/viaje/saveFavorite")]
        public ResSaveFavorite SaveFavorite(ReqSaveFavorite req)
        {
            return new ViajeLogic().SaveFavorite(req);
        }

        [HttpGet]
        [Route("api/viaje/getFavorites/{guid}")]
        public ResGetFavorites GetFavorites(Guid guid)
        {
            return new ViajeLogic().GetFavorites(new ReqGetFavorites { guid = guid });
        }

        [HttpDelete]
        [Route("api/viaje/deleteHistory/{guid}/{idHistorial}")]
        public ResDeleteHistory DeleteHistory(Guid guid, long idHistorial)
        {
            return new ViajeLogic().deleteHistory(new ReqDeleteHistory { guid = guid, idHistorial = idHistorial });
        }

        [HttpPost]
        [Route("api/viaje/deleteFavorite")]
        public ResDeleteFavorite DeleteFavorite(ReqDeleteFavorite req)
        {
            return new ViajeLogic().DeleteFavorite(req);
        }
    }
}