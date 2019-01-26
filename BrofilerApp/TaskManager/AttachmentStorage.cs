using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.TaskManager
{
	public abstract class ExtrernalStorage
	{
		public abstract Uri UploadFile(String name, System.IO.Stream data);
	}

    class GDriveStorage : ExtrernalStorage
	{
		const string SERVICE_ACCOUNT_EMAIL = "upload@brofiler-github.iam.gserviceaccount.com";
		const string KEY_RESOURCE_NAME = "Profiler.TaskManager.brofiler-github-07994fd14248.p12";

		private DriveService Connect()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			using (System.IO.Stream stream = assembly.GetManifestResourceStream(KEY_RESOURCE_NAME))
			{
				string[] scopes = new string[] { DriveService.Scope.Drive };

				byte[] key = new byte[stream.Length];
				stream.Read(key, 0, (int)stream.Length);

				var certificate = new X509Certificate2(key, "notasecret", X509KeyStorageFlags.Exportable);
				var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(SERVICE_ACCOUNT_EMAIL)
				{
					Scopes = scopes
				}.FromCertificate(certificate));

				return new DriveService(new BaseClientService.Initializer()
				{
					HttpClientInitializer = credential,
					ApplicationName = "Brofiler Github Sample",
				});
			}
		}

		private static string GetMimeType(string fileName)
		{
			string mimeType = "application/unknown";
			string ext = System.IO.Path.GetExtension(fileName).ToLower();
			Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
			if (regKey != null && regKey.GetValue("Content Type") != null)
				mimeType = regKey.GetValue("Content Type").ToString();
			return mimeType;
		}

		private String UploadFile(DriveService service, String name, System.IO.Stream stream)
		{
			File body = new File();
			body.Name = System.IO.Path.GetFileName(name);
			body.Description = "File uploaded by Brofiler";
			body.MimeType = GetMimeType(name);

			// File's content.
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0;

			try
			{
				var uploadRequest = service.Files.Create(body, memoryStream, GetMimeType(name));
				uploadRequest.Fields = "id";
				uploadRequest.Upload();

				String fileId = uploadRequest.ResponseBody.Id;

				Permission userPermission = new Permission()
				{
					Type = "anyone",
					Role = "reader",
					AllowFileDiscovery = false,
				};
				var permissionsRequest = service.Permissions.Create(userPermission, fileId);
				permissionsRequest.Execute();

				Permission domainPermission = new Permission()
				{
					Type = "domain",
					Role = "reader",
				};
				permissionsRequest = service.Permissions.Create(domainPermission, fileId);
				permissionsRequest.Execute();

				return fileId;

			}
			catch (Exception e)
			{
				Debug.WriteLine("An error occurred: " + e.Message);
				return null;
			}
		}

		public override Uri UploadFile(String name, System.IO.Stream data)
		{
			DriveService service = Connect();

			if (service != null)
			{
				String fileId = UploadFile(service, name, data);
				if (!String.IsNullOrEmpty(fileId))
				{
					return new Uri("https://drive.google.com/uc?id=" + fileId);
				}
			}

			return null;
		}
    }
}
