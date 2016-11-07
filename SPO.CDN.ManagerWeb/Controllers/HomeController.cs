﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.Online.SharePoint.TenantManagement;
using Microsoft.SharePoint.Client;
using SPO.CDN.ManagerWeb.DTO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security;
using System.Web.Mvc;

namespace SPO.CDN.ManagerWeb.Controllers
{
    public class HomeController : Controller
    {


        [SharePointContextFilter]
        public ActionResult Index()
        {
            return View();
        }

        [SharePointContextFilter]
        public ActionResult GetCDNSettings()
        {
            var cdnManagerModel = new CDNManagerModel();

            using (var clientContext = GetClientContext())
            {
                var tenant = new Office365Tenant(clientContext);
                
                clientContext.Load(tenant,
                    t => t.PublicCdnEnabled,
                    t => t.PublicCdnAllowedFileTypes,
                    t => t.PublicCdnOrigins);

                clientContext.ExecuteQuery();


                cdnManagerModel.PublicCDNEnabled = tenant.PublicCdnEnabled;
                cdnManagerModel.Filetypes = ConvertToList(tenant.PublicCdnAllowedFileTypes);
                cdnManagerModel.Origins = GetCDNOrigins(tenant.PublicCdnOrigins);

            }

            return Json(cdnManagerModel, JsonRequestBehavior.AllowGet);
        }

        [SharePointContextFilter]
        public ActionResult SetCDN(bool value)
        {
            bool CDNEnabled = false;

            using (var clientContext = GetClientContext())
            {
                var tenant = new Office365Tenant(clientContext);

                tenant.PublicCdnEnabled = value;

                clientContext.Load(tenant, t => t.PublicCdnEnabled);

                clientContext.ExecuteQuery();

                CDNEnabled = tenant.PublicCdnEnabled;

            }

            return Json(CDNEnabled, JsonRequestBehavior.AllowGet);
        }

        [SharePointContextFilter]
        public ActionResult RemoveOrigin(string originID)
        {
            IList<CDNOrigin> origins = new List<CDNOrigin>();

            using (var clientContext = GetClientContext())
            {
                var tenant = new Office365Tenant(clientContext);

                tenant.RemovePublicCdnOrigin(originID);

                clientContext.Load(tenant, t => t.PublicCdnOrigins);

                clientContext.ExecuteQuery();

                origins = GetCDNOrigins(tenant.PublicCdnOrigins);
            }

            return Json(origins, JsonRequestBehavior.AllowGet);
        }

        [SharePointContextFilter]
        [HttpPost]
        public ActionResult AddOrigin(string folderUrl)
        {
            try
            {
                IList<CDNOrigin> origins = new List<CDNOrigin>();

                using (var clientContext = GetClientContext())
                {
                    var tenant = new Office365Tenant(clientContext);

                    tenant.AddPublicCdnOrigin(folderUrl);

                    clientContext.Load(tenant, t => t.PublicCdnOrigins);

                    clientContext.ExecuteQuery();

                    origins = GetCDNOrigins(tenant.PublicCdnOrigins);
                }

                return Json(origins);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(ex.Message);
            }
        }

        [SharePointContextFilter]
        [HttpPost]
        public ActionResult SetFiletypes(List<string> filetypes)
        {
            try
            {
                string cdnFileTypes;

                using (var clientContext = GetClientContext())
                {
                    var tenant = new Office365Tenant(clientContext);

                    var newFileTypes = string.Join(",", filetypes);

                    tenant.PublicCdnAllowedFileTypes = newFileTypes;

                    clientContext.Load(tenant, t => t.PublicCdnAllowedFileTypes);

                    clientContext.ExecuteQuery();

                    cdnFileTypes = tenant.PublicCdnAllowedFileTypes;
                }

                return Json(ConvertToList(cdnFileTypes));
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(ex.Message);
            }
        }

        private ClientContext GetClientContext()
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(HttpContext);

            var clientContext = spContext.CreateUserClientContextForSPHost();

            return clientContext;
        }

        private IList<CDNOrigin> GetCDNOrigins(IList<string> publicCdnOrigins)
        {
            var cdnOrigins = new List<CDNOrigin>();

            foreach (string origin in publicCdnOrigins)
            {
                var splitOrigin = origin.Split(',');
                var cdnOrigin = new CDNOrigin();
                cdnOrigin.Url = splitOrigin[0];
                cdnOrigin.ID = splitOrigin[1];

                cdnOrigins.Add(cdnOrigin);
            }

            return cdnOrigins;
        }

        private IList<string> ConvertToList(string publicCdnAllowedFileTypes)
        {
            return publicCdnAllowedFileTypes.Split(',').ToList();
        }
    }
}
