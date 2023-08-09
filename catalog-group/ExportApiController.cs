using EPiServer.Cms.UI.AspNetIdentity;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Catalog.ImportExport;
using Mediachase.Commerce.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("importer")]
    public class ImporterApiController : ControllerBase
    {
        public ImporterApiController()
        {

        }

        [HttpGet]
        [Route("export")]
        public async Task<ActionResult<string>> Export([FromQuery] string catalogName = null)
        {
            catalogName = catalogName ?? "Test";
            var log = "";
            CatalogImportExport _importExport = new CatalogImportExport();
            FileStream fs = BuildExportPath();
            log += (fs.Name) + "\n";
            log += (Path.GetDirectoryName(fs.Name));
            _importExport.Export(catalogName, fs, Path.GetDirectoryName(fs.Name));

            return Ok(log);
        }

        private FileStream BuildExportPath()
        {
            StringBuilder sbDirName = new StringBuilder(Path.GetTempPath());
            sbDirName.AppendFormat("CatalogExport_test");
            string dirName = sbDirName.ToString();
            if (Directory.Exists(dirName))
                Directory.Delete(dirName, true);
            DirectoryInfo dir = Directory.CreateDirectory(dirName);
            StringBuilder filePath = new StringBuilder(dir.FullName);
            filePath.AppendFormat("\\Catalog.xml");
            FileStream fs = new FileStream(filePath.ToString(), FileMode.Create, FileAccess.ReadWrite);
            return fs;
        }
    }
}