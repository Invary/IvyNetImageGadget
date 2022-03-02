using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Invary.Utility
{
	class DownEventArgs : EventArgs
	{
		public Stream? stream { set; get; } = null;
		public bool Succeeded { set; get; } = false;

		public DownEventArgs()
		{
		}

		public DownEventArgs(Stream stream, bool Succeeded)
		{
			this.stream = stream;
			this.Succeeded = Succeeded;
		}
	}



	class DownloadStreamTask : IDisposable
	{
		Task? _task = null;
		HttpClient? _client = null;

		CancellationTokenSource? _cancellationTokenSource = null;
		CancellationToken _token;


		public string Url { set; get; } = "";
		public string Referer { set; get; } = "";
		public string UserAgent { set; get; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36";

		public double TimeoutSec { set; get; } = 0;
		public object? Tag { set; get; } = null;

		public EventHandler<DownEventArgs>? OnDownloadCompleted { set; get; } = null;


		public bool IsRunning
		{
			get
			{
				if (_task == null || _client == null)
					return false;

				if (_task.Status == TaskStatus.Running)
					return true;
				if (_task.Status == TaskStatus.WaitingToRun)
					return true;
				if (_task.Status == TaskStatus.WaitingForActivation)
					return true;
				if (_task.Status == TaskStatus.WaitingForChildrenToComplete)
					return true;

				//if (_task.IsCompleted || _task.IsCanceled || _task.IsFaulted)
				return false;
			}
		}


		public bool Start()
		{
			if (_task != null)
				return false;

			_cancellationTokenSource = new CancellationTokenSource();
			_token = _cancellationTokenSource.Token;
			_task = Task.Run(mainAsync);

			return true;
		}



		public void Stop()
		{
			if (_task == null)
				return;

			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();
			_task.WaitAsync(new TimeSpan(0, 0, 0, 0, 500));

			try
			{
				if (_client != null)
					_client.Dispose();
			}
			catch (Exception)
			{
			}

			try
			{
				_task.Dispose();
			}
			catch (Exception)
			{
			}
			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Dispose();
			_cancellationTokenSource = null;

			_client = null;
			_task = null;
		}






		public void Dispose()
		{
			Stop();
		}


		async Task mainAsync()
		{
			if (_token.IsCancellationRequested)
				return;

			using (_client = new HttpClient())
			{
				_client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
				//_client.DefaultRequestHeaders.Add("Accept-Language", "ja-JP");

				if (string.IsNullOrEmpty(Referer) == false)
					_client.DefaultRequestHeaders.Referrer = new Uri(Referer);

				if (TimeoutSec > 0)
					_client.Timeout = TimeSpan.FromSeconds(TimeoutSec);

				if (_token.IsCancellationRequested)
					return;

				using (var response = await _client.GetAsync(Url, _token))
				using (var stream = response.Content.ReadAsStreamAsync(_token).Result)
				{
					if (_token.IsCancellationRequested)
						return;

					bool ret = response.IsSuccessStatusCode;
					OnDownloadCompleted?.Invoke(this, new DownEventArgs(stream, ret));
				}
			}
			_client = null;
		}
	}

}
