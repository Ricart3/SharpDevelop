﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ICSharpCode.AddInManager2.Model;
using ICSharpCode.SharpDevelop;
using NuGet;

namespace ICSharpCode.AddInManager2.ViewModel
{
	public abstract class AddInsViewModelBase : Model<AddInsViewModelBase>, IDisposable
	{
		private Pages _pages;
		private int _highlightCount;
		private string _title;
		private PackageRepository _activePackageSource;
		private IPackageRepository _activePackageRepository;
		private bool _isReadingPackages;
		private bool _isExpandedinView;
		private bool _showPackageSources;
		
		private ObservableCollection<PackageRepository> _packageRepositories;
		
		public event EventHandler AddInsListUpdated;
		
		public AddInsViewModelBase()
			:base()
		{
			Initialize();
		}
		
		public AddInsViewModelBase(IAddInManagerServices services)
			: base(services)
		{
			Initialize();
		}
		
		private void Initialize()
		{
			_activePackageRepository = null;
			_isReadingPackages = false;
			_isExpandedinView = false;
			
			// Initialization of internal lists
			_pages = new Pages();
			_highlightCount = 0;
			AddInPackages = new ObservableCollection<AddInPackageViewModelBase>();
			_packageRepositories = new ObservableCollection<PackageRepository>();
			ErrorMessage = String.Empty;
			
			// Update package sources list and ensure that it's updated automatically from now
			UpdatePackageSources();
			AddInManager.Events.PackageSourcesChanged += AddInManager_Events_PackageSourcesChanged;
			
			CreateCommands();
		}
		
		private void CreateCommands()
		{
			ShowNextPageCommand = new DelegateCommand(param => ShowNextPage());
			ShowPreviousPageCommand = new DelegateCommand(param => ShowPreviousPage());
			ShowPageCommand = new DelegateCommand(param => ExecuteShowPageCommand(param));
			SearchCommand = new DelegateCommand(param => Search());
			UpdatePreinstalledFilterCommand = new DelegateCommand(param => UpdatePreinstalledFilter());
			UpdatePrereleaseFilterCommand = new DelegateCommand(param => UpdatePrereleaseFilter());
			InstallFromArchiveCommand = new DelegateCommand(param => InstallFromArchive());
		}
		
				private void OnAddInsListUpdated()
		{
			if (AddInsListUpdated != null)
			{
				AddInsListUpdated(this, new EventArgs());
			}
		}
		
		public ICommand ShowNextPageCommand
		{
			get;
			private set;
		}
		
		public ICommand ShowPreviousPageCommand
		{
			get;
			private set;
		}
		
		public ICommand ShowPageCommand
		{
			get;
			private set;
		}
		
		public ICommand SearchCommand
		{
			get;
			private set;
		}
		
		public ICommand UpdatePreinstalledFilterCommand
		{
			get;
			private set;
		}
		
		public ICommand UpdatePrereleaseFilterCommand
		{
			get;
			private set;
		}
		
		public ICommand InstallFromArchiveCommand
		{
			get;
			private set;
		}
		
		public void Dispose()
		{
			OnDispose();
			IsDisposed = true;
		}
		
		protected virtual void OnDispose()
		{
		}
		
		public bool IsDisposed
		{
			get;
			protected set;
		}
		
		public bool HasError
		{
			get;
			protected set;
		}
		
		public string ErrorMessage
		{
			get;
			protected set;
		}
		
		public int HighlightCount
		{
			get
			{
				return _highlightCount;
			}
			protected set
			{
				_highlightCount = value;
				OnPropertyChanged(vm => vm.HighlightCount);
				OnPropertyChanged(vm => vm.HasHighlightCount);
				OnPropertyChanged(vm => vm.TitleWithHighlight);
			}
		}
		
		public bool HasHighlightCount
		{
			get
			{
				return (_highlightCount > 0);
			}
		}
		
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				OnPropertyChanged(vm => vm.HighlightCount);
				OnPropertyChanged(vm => vm.HasHighlightCount);
				OnPropertyChanged(vm => vm.TitleWithHighlight);
			}
		}
		
		public string TitleWithHighlight
		{
			get
			{
				if (_highlightCount > 0)
				{
					return String.Format("{0} ({1})", Title, _highlightCount);
				}
				else
				{
					return Title;
				}
			}
		}
		
		public ObservableCollection<AddInPackageViewModelBase> AddInPackages
		{
			get;
			private set;
		}
		
		public ObservableCollection<Page> Pages
		{
			get
			{
				return _pages;
			}
		}
		
		protected Pages PagesCollection
		{
			get
			{
				return _pages;
			}
		}
		
		public bool IsReadingPackages
		{
			get
			{
				return _isReadingPackages;
			}
			protected set
			{
				_isReadingPackages = value;
				OnPropertyChanged(m => m.IsReadingPackages);
			}
		}
		
		public bool IsExpandedInView
		{
			get
			{
				return _isExpandedinView;
			}
			set
			{
				_isExpandedinView = value;
				OnPropertyChanged(m => m.IsExpandedInView);
			}
		}
		
		public virtual void ReadPackages()
		{
			_pages.SelectedPageNumber = 1;
		}
		
		protected void SaveError(AggregateException ex)
		{
			HasError = true;
			ErrorMessage = GetErrorMessage(ex);
			ICSharpCode.Core.LoggingService.Debug(ex);
			
			SD.Log.DebugFormatted("[AddInManager2] Showing error: {0}", ex.Message);
		}
		
		protected string GetErrorMessage(AggregateException ex)
		{
			StringBuilder errorMessage = new StringBuilder();
			BuildErrorMessage(ex.InnerExceptions, errorMessage);
			return errorMessage.ToString().TrimEnd();
		}
		
		private void BuildErrorMessage(IEnumerable<Exception> exceptions, StringBuilder errorMessage)
		{
			foreach (Exception ex in exceptions)
			{
				var aggregateEx = ex as AggregateException;
				if (aggregateEx != null)
				{
					BuildErrorMessage(aggregateEx.InnerExceptions, errorMessage);
				}
				else
				{
					errorMessage.AppendLine(ex.Message);
				}
			}
		}
		
		protected void UpdatePackageViewModels(IEnumerable<AddInPackageViewModelBase> newPackageViewModels)
		{
			ClearPackages();
			NuGet.CollectionExtensions.AddRange(AddInPackages, newPackageViewModels);
			UpdateInstallationState();
		}
		
		protected void ClearPackages()
		{
			AddInPackages.Clear();
		}
		
		public virtual int SelectedPageNumber
		{
			get
			{
				return _pages.SelectedPageNumber;
			}
			set
			{
				if (_pages.SelectedPageNumber != value)
				{
					_pages.SelectedPageNumber = value;
				}
			}
		}
		
		public int PageSize
		{
			get
			{
				return _pages.PageSize;
			}
			set
			{
				_pages.PageSize = value;
			}
		}
		
		public bool IsPaged
		{
			get
			{
				return _pages.IsPaged;
			}
		}
		
		public bool HasPreviousPage
		{
			get
			{
				return _pages.HasPreviousPage;
			}
		}
		
		public bool HasNextPage
		{
			get
			{
				return _pages.HasNextPage;
			}
		}
		
		public int MaximumSelectablePages
		{
			get
			{
				return _pages.MaximumSelectablePages;
			}
			set
			{
				_pages.MaximumSelectablePages = value;
			}
		}
		
		public int TotalItems
		{
			get;
			protected set;
		}
		
		public void ShowNextPage()
		{
			SelectedPageNumber += 1;
		}
		
		public void ShowPreviousPage()
		{
			SelectedPageNumber -= 1;
		}
		
		private void ExecuteShowPageCommand(object param)
		{
			int pageNumber = (int)param;
			ShowPage(pageNumber);
		}
		
		public void ShowPage(int pageNumber)
		{
			SelectedPageNumber = pageNumber;
		}
		
		public bool IsSearchable
		{
			get;
			set;
		}
		
		public bool HasFilterForPreinstalled
		{
			get;
			set;
		}
		
		public bool HasFilterForPrereleases
		{
			get;
			set;
		}
		
		public bool AllowInstallFromArchive
		{
			get;
			set;
		}
		
		public string SearchTerms
		{
			get;
			set;
		}
		
		public void Search()
		{
			ReadPackages();
			OnPropertyChanged(null);
		}
		
		protected virtual void UpdatePreinstalledFilter()
		{
		}
		
		protected virtual void UpdatePrereleaseFilter()
		{
		}
		
		protected virtual void InstallFromArchive()
		{
		}
		
		public bool ShowPackageSources
		{
			get
			{
				return _showPackageSources;
			}
			set
			{
				_showPackageSources = value;
				UpdatePackageSources();
			}
		}
		
		public ObservableCollection<PackageRepository> PackageRepositories
		{
			get
			{
				return _packageRepositories;
			}
		}
		
		private void UpdatePackageSources()
		{
			if (!ShowPackageSources)
			{
				return;
			}
			
			PackageRepository oldValue = SelectedPackageSource;
			
			// Refill package sources list
			_packageRepositories.Clear();
			foreach (PackageSource packageSource in AddInManager.Repositories.RegisteredPackageSources)
			{
				_packageRepositories.Add(new PackageRepository(AddInManager, packageSource));
			}
			
			// Try to select the same active source, again
			if ((oldValue != null) && _packageRepositories.Contains(oldValue) && (oldValue != SelectedPackageSource))
			{
				SelectedPackageSource = oldValue;
			}
			else
			{
				// Select first source
				SelectedPackageSource = _packageRepositories.FirstOrDefault();
			}
		}
		
		public PackageRepository SelectedPackageSource
		{
			get
			{
				return _activePackageSource;
			}
			set
			{
				SD.Log.Debug("[AddInManager2] AddInsViewModelBase: Changed package source");
				
				_activePackageSource = value;
				if (_activePackageSource != null)
				{
					_activePackageRepository = AddInManager.Repositories.GetRepositoryFromSource(_activePackageSource.ToPackageSource());
				}
				else
				{
					_activePackageRepository = null;
				}
				ReadPackages();
				OnPropertyChanged(m => m.SelectedPackageSource);
			}
		}
		
		public IPackageRepository ActiveRepository
		{
			get
			{
				return _activePackageRepository;
			}
		}
		
		public bool ShowPreinstalledAddIns
		{
			get;
			set;
		}
		
		public bool ShowPrereleases
		{
			get;
			set;
		}
		
		public void UpdateInstallationState()
		{
			// Update installation-state-related properties of all AddIn items in here
			foreach (var packageViewModel in AddInPackages)
			{
				packageViewModel.UpdateInstallationState();
			}
		}
		
		
		private void AddInManager_Events_PackageSourcesChanged(object sender, EventArgs e)
		{
			// Update the list of package sources
			UpdatePackageSources();
		}
	}
}