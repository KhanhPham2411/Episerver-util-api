using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using Foundation.Features.CatalogContent.Product;
using Mediachase.Commerce.Catalog;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace Foundation.Custom
{
    [ApiController]
    [Route("media-api")]
    public class MediaApiController : ControllerBase
    {
        private readonly IContentLoader _contentLoader;


        public MediaApiController(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        private FileContentResult DownloadInternal(ContentReference contentRef)
        {
            var mediaData = _contentLoader.Get<IContent>(contentRef) as MediaData;
            if (mediaData != null)
            {
                var downloadFile = mediaData;
                if (downloadFile != null)
                {
                    var blob = downloadFile.BinaryData as FileBlob;
                    if (blob != null)
                    {
                        var routeSegment = downloadFile.RouteSegment;
                        var extension = Path.GetExtension(blob.FilePath) ?? "";
                        var downloadFileName = routeSegment.EndsWith(extension) ? routeSegment : routeSegment + extension;

                        HttpContext.Response.Headers.Add("content-disposition", "attachment;filename=" + Path.GetFileName(downloadFileName));
                        return File(System.IO.File.ReadAllBytes(blob.FilePath), "application/octet-stream");
                    }
                }
            }
            return null;
        }


        [HttpGet("DownloadProductImage/{contentLinkId}")]
        public IActionResult DownloadProductImage(string productCode)
        {
            var _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();

            var productContentLink = _referenceConverter.GetContentLink(productCode ?? "p-39813617");
            var product = _contentRepository.Get<GenericProduct>(productContentLink).CreateWritableClone<GenericProduct>();

            var firstImage = product.CommerceMediaCollection.First().AssetLink;
            return DownloadInternal(firstImage);
        }


        [HttpGet("Download/{contentLinkId}")]
        public IActionResult Download(int contentLinkId)
        {
            return DownloadInternal(new ContentReference(contentLinkId));
        }
    }
}