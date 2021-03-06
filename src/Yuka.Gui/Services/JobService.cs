﻿using System;
using System.Threading.Tasks;
using System.Windows;
using Yuka.Gui.Jobs;
using Yuka.Gui.Services.Abstract;

namespace Yuka.Gui.Services {
	public class JobService : IJobService {

		protected readonly Window MainWindow;

		public JobService(Window mainWindow) {
			MainWindow = mainWindow;
		}

		public void QueueJob(Job job) {
			// TODO async execution
			Task.Run((Action)job.Execute);
		}
	}
}
