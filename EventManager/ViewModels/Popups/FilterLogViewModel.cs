﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Models;
using EventManager.Services;
using Mopups.PreBaked.PopupPages.Login;
using Mopups.Services;


namespace EventManager.ViewModels.Popups
{
    public partial class FilterLogViewModel: ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly LogsViewModel logsViewModel;
        private LogFilter logFilter;

        [ObservableProperty]
        private string selectedName;

        [ObservableProperty]
        private string selectedCategory;

        [ObservableProperty]
        private string selectedDate;

        [ObservableProperty]
        private string selectedTime;

        [ObservableProperty] 
        private ObservableCollection<string> eventNames = new();

        [ObservableProperty] 
        private ObservableCollection<string> eventCategories = new();

        [ObservableProperty] 
        private ObservableCollection<string> eventDates = new();

        [ObservableProperty] 
        private ObservableCollection<string> eventTimes = new();

        [ObservableProperty]
        private bool isCategoryEnabled;

        [ObservableProperty]
        private bool isDateEnabled;

        [ObservableProperty]
        private bool isTimeEnabled;

        [ObservableProperty]
        private string placeholderColor;

        public FilterLogViewModel(DatabaseService databaseService, LogsViewModel logsViewModel, LogFilter logFilter) 
        {
            this.logsViewModel = logsViewModel;
            this.databaseService = databaseService;
            this.logFilter = logFilter;
            Task.Run(async () => await LoadDefaultValues());
        }

        private async Task LoadEventNames()
        {
            var names = await databaseService.GetDistinctLogValues("EventName");
            EventNames = new ObservableCollection<string>(names);
        }

        private async Task LoadDefaultValues()
        {
            await LoadEventNames();

            SelectedName = logFilter.Name;
  
            var categories = await databaseService.GetFilteredCategories(SelectedName);
            EventCategories = new ObservableCollection<string>(categories);
            SelectedCategory = logFilter.Category;

            var dates = await databaseService.GetFilteredDates(SelectedName, SelectedCategory);
            EventDates = new ObservableCollection<string>(dates);
            SelectedDate = logFilter.Date;

            var times = await databaseService.GetFilteredTimes(SelectedName, SelectedCategory, SelectedDate);
            EventTimes = new ObservableCollection<string>(times);
            SelectedTime = logFilter.Time;
        }

        partial void OnSelectedNameChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
    
            IsCategoryEnabled = true;
            IsDateEnabled = false;
            IsTimeEnabled = false;

            SelectedCategory = null;
            SelectedDate = null;
            SelectedTime = null;

            LoadCategories(value);
            }
            else
            {
  
            IsCategoryEnabled = false;
            IsDateEnabled = false;
            IsTimeEnabled = false;
            }
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
          
            IsDateEnabled = true;
            IsTimeEnabled = false;

            SelectedDate = null;
            SelectedTime = null;

            LoadDates(SelectedName, value);
            }
            else
            {
           
            IsDateEnabled = false;
            IsTimeEnabled = false;
            }
        }

        partial void OnSelectedDateChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
            IsTimeEnabled = true;
            SelectedTime = null;
            LoadTimes(SelectedName, SelectedCategory, value);
            }
            else
            {
            IsTimeEnabled = false;
            }
        }

        private async void LoadCategories(string eventName)
        {
            var categories = await databaseService.GetFilteredCategories(eventName);
            EventCategories = new ObservableCollection<string>(categories);
        }

        private async void LoadDates(string eventName, string category)
        {
            var dates = await databaseService.GetFilteredDates(eventName, category);
            EventDates = new ObservableCollection<string>(dates);
        }

        private async void LoadTimes(string eventName, string category, string date)
        {
            var times = await databaseService.GetFilteredTimes(eventName, category, date);
            EventTimes = new ObservableCollection<string>(times);
        }

        [RelayCommand]
        private async Task ApplyLogFilter()
        {
            var filterModel = new LogFilter
            {
                Name = SelectedName,
                Category = SelectedCategory,
                Date = SelectedDate,
                Time = SelectedTime
            };

            await MopupService.Instance.PopAsync();
            await logsViewModel.ApplyFilterLogs(filterModel);
        
        }
    }
}
