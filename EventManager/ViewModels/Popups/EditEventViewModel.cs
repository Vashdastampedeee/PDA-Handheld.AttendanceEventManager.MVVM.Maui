﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventManager.Services;
using EventManager.Utilities;
using Mopups.Services;
using System.Diagnostics;

namespace EventManager.ViewModels.Popups
{
    public partial class EditEventViewModel : ObservableObject
    {
        private readonly DatabaseService databaseService;
        private readonly EventViewModel eventViewModel;
        private readonly int eventId;
        private byte[] eventImageData;
        public List<string> CategoryOptions { get; } = new() {"Company Event", "Orientation", "Seminar", "Training" };

        [ObservableProperty] private string eventName;
        [ObservableProperty] private string selectedCategory;
        [ObservableProperty] private DateTime minimumEventData = DateTime.Today;
        [ObservableProperty] private DateTime eventDate;
        [ObservableProperty] private TimeSpan fromTime;
        [ObservableProperty] private TimeSpan toTime;
        [ObservableProperty] private bool isToTimeEnabled;
        [ObservableProperty] private ImageSource eventImagePreview = "event_image_placeholder.jpg";

        public EditEventViewModel(DatabaseService databaseService, EventViewModel eventViewModel ,int eventId)
        {
            this.databaseService = databaseService;
            this.eventViewModel = eventViewModel;
            this.eventId = eventId;
            LoadEventData();
        }

        private async void LoadEventData()
        {
            var selectedEvent = await databaseService.GetEventById(eventId);
            if (selectedEvent != null)
            {
                EventName = selectedEvent.EventName;
                SelectedCategory = selectedEvent.EventCategory;
                if (DateTime.TryParseExact(selectedEvent.EventDate, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    EventDate = parsedDate;
                }
                else
                {
                    EventDate = DateTime.Today;
                }
                if (DateTime.TryParseExact(selectedEvent.EventFromTime, "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedFromTime))
                {
                    FromTime = parsedFromTime.TimeOfDay;
                    
                }
                else
                {
                    FromTime = TimeSpan.Zero;
                }
                if (DateTime.TryParseExact(selectedEvent.EventToTime, "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedToTime))
                {
                    ToTime = parsedToTime.TimeOfDay;
                    
                }
                else
                {
                    ToTime = TimeSpan.Zero;
                }
                eventImageData = selectedEvent.EventImage;
                EventImagePreview = ImageHelper.ConvertBytesToImage(eventImageData);
            }
        }

        [RelayCommand]
        private async Task SubmitEdit()
        {
            if (string.IsNullOrEmpty(EventName))
            {
                await ToastHelper.ShowToast("Event Name is required!", ToastDuration.Short);
                return;
            }
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                await ToastHelper.ShowToast("Event Category is required!", ToastDuration.Short);
                return;
            }
            if (FromTime == TimeSpan.Zero)
            {
                await ToastHelper.ShowToast("Event Time is required!", ToastDuration.Short);
                return;
            }

            string formattedEventDate = EventDate.ToString("MM/dd/yyyy");
            string formattedFromTime = DateTime.Today.Add(FromTime).ToString("hh:mm tt");
            string formattedToTime = DateTime.Today.Add(ToTime).ToString("hh:mm tt");


            var selectedEvent = await databaseService.GetEventById(eventId);
            bool isEventChanged = EventName.Trim() != selectedEvent.EventName || SelectedCategory != selectedEvent.EventCategory || formattedEventDate != selectedEvent.EventDate || formattedFromTime != selectedEvent.EventFromTime || formattedToTime != selectedEvent.EventToTime;

            if (isEventChanged)
            {
                bool isExistingEvent = await databaseService.IsExistingEvent(EventName.Trim(), SelectedCategory, formattedEventDate, formattedFromTime, formattedToTime);
                if (isExistingEvent)
                {
                    await ToastHelper.ShowToast("Event with the same time already exists!", ToastDuration.Short);
                    return;
                }
            }
 
            await databaseService.UpdateSelectedEvent(eventId, EventName, SelectedCategory, eventImageData, formattedEventDate, formattedFromTime, formattedToTime);
            await MopupService.Instance.PopAsync();
            await eventViewModel.RefreshEvents();
            await ToastHelper.ShowToast("Event Updated", ToastDuration.Short);
            
        }

        [RelayCommand]
        private async Task UploadImage()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    eventImageData = memoryStream.ToArray();
                }

                EventImagePreview = ImageSource.FromStream(() => new MemoryStream(eventImageData));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditEventViewModel] Error: {ex.Message}");
            }
        }

        partial void OnFromTimeChanged(TimeSpan value)
        {
            if (value == TimeSpan.Zero)
            {
                IsToTimeEnabled = false;
            }
            else
            {
                IsToTimeEnabled = true;

                if (ToTime <= value)
                {
                    ToTime = value.Add(TimeSpan.FromHours(1));
                }
            }
        }

        partial void OnToTimeChanged(TimeSpan value)
        {
            if (value <= FromTime)
            {
                ToTime = FromTime.Add(TimeSpan.FromHours(1));
            }
        }
    }
}
