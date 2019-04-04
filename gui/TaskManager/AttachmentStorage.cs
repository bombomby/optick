using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Profiler.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Profiler.TaskManager
{
	public abstract class ExternalStorage
	{
		public abstract String DisplayName { get; }
		public abstract String Icon { get; }
		public abstract bool IsPublic { get; }

		public abstract Uri UploadFile(String name, System.IO.Stream data, Action<double> onProgress, CancellationToken token);
	}

    class GDriveStorage : ExternalStorage
	{
		public override string DisplayName => "Public Google Drive Storage (Optick)";
		public override string Icon => "appbar_google";
		public override bool IsPublic => true;

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
					ApplicationName = "Optick Github Sample",
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

		private String UploadFile(DriveService service, String name, System.IO.Stream stream, Action<double> onProgress, CancellationToken token)
		{
			File body = new File();
			body.Name = System.IO.Path.GetFileName(name);
			body.Description = "File uploaded by Optick";
			body.MimeType = GetMimeType(name);

			// File's content.
			System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
			stream.Position = 0;
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0;

			try
			{
				var uploadRequest = service.Files.Create(body, memoryStream, GetMimeType(name));
				uploadRequest.ProgressChanged += (p) => onProgress?.Invoke((double)p.BytesSent / stream.Length);
				uploadRequest.Fields = "id";
				uploadRequest.ChunkSize = Google.Apis.Upload.ResumableUpload.MinimumChunkSize;
				uploadRequest.UploadAsync(token).Wait(token);

				String fileId = uploadRequest.ResponseBody.Id;

				Permission userPermission = new Permission()
				{
					Type = "anyone",
					Role = "reader",
					AllowFileDiscovery = false,
				};
				var permissionsRequest = service.Permissions.Create(userPermission, fileId);
				permissionsRequest.Execute();

				//Permission domainPermission = new Permission()
				//{
				//	Type = "domain",
				//	Role = "reader",

				//};
				//permissionsRequest = service.Permissions.Create(domainPermission, fileId);
				//permissionsRequest.Execute();

				return fileId;

			}
			catch (Exception e)
			{
				Debug.WriteLine("An error occurred: " + e.Message);
				return null;
			}
		}

		public override Uri UploadFile(String name, System.IO.Stream data, Action<double> onProgress, CancellationToken token)
		{
			DriveService service = Connect();

			if (service != null)
			{
				String fileId = UploadFile(service, name, data, onProgress, token);
				if (!String.IsNullOrEmpty(fileId))
				{
					return new Uri("https://drive.google.com/uc?id=" + fileId);
				}
			}

			return null;
		}
    }

	class NetworkStorage : ExternalStorage
	{
		public String UploadURL { get; set; }
		public String DownloadURL { get; set; }

		public String GUID { get; set; } = Utils.GenerateShortGUID();
		public DateTime Date { get; set; } = DateTime.Now;

		public String IntermediateFolder => String.Format("{0}/{1}", Date.ToString("yyyy-MM-dd"), GUID);

		public override string DisplayName => String.Format("{0} ({1})", DownloadURL, UploadURL);
		public override string Icon => "appbar_folder";
		public override bool IsPublic => false;

		public NetworkStorage(String uploadURL, String downloadURL)
		{
			UploadURL = uploadURL;
			DownloadURL = downloadURL;
		}

		public override Uri UploadFile(string name, System.IO.Stream data, Action<double> onProgress, CancellationToken token)
		{
			String uploadFolder = System.IO.Path.Combine(UploadURL, IntermediateFolder);
			System.IO.Directory.CreateDirectory(uploadFolder);

			String uploadPath = System.IO.Path.Combine(uploadFolder, name);
			data.Position = 0;
			using (System.IO.FileStream outputStream = new System.IO.FileStream(uploadPath, System.IO.FileMode.Create))
				Utils.CopyStream(data, outputStream, (p) => { token.ThrowIfCancellationRequested(); onProgress(p); });

			String downloadPath = String.Format("{0}/{1}/{2}", DownloadURL, IntermediateFolder, name);
			return new Uri(downloadPath);
		}
	}
}
