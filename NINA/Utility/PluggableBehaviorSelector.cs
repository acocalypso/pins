#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace NINA.Utility {
    /// <summary>
    /// A PluggableBehaviorSelector instance exists for each pluggable interface. It encapsulates a list of default and plugged instances as well as saving/loading
    /// the selected type from the profile, 
    /// </summary>
    /// <typeparam name="T">The pluggable interface type</typeparam>
    /// <typeparam name="DefaultT">The default implementation type provided by NINA</typeparam>
    public class PluggableBehaviorSelector<T, DefaultT> : BaseINPC, IPluggableBehaviorSelector<T>
        where T : class, IPluggableBehavior
        where DefaultT : T {
        private readonly IProfileService profileService;
        private readonly DefaultT ninaDefault;
        private string selectedContentId;

        /// <summary>
        /// pins forces HocusFocus for these interfaces whenever it is loaded (there is no options UI to pick
        /// a behavior on a headless install). Keyed by interface full name, valued by the forced ContentId.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> ForcedHocusFocusBehaviors = new Dictionary<string, string> {
            { "NINA.Image.ImageAnalysis.IStarDetection", "NINA.Joko.Plugins.HocusFocus.StarDetection.HocusFocusStarDetection" },
            { "NINA.Image.ImageAnalysis.IStarAnnotator", "NINA.Joko.Plugins.HocusFocus.StarDetection.HocusFocusStarAnnotator" },
            { "NINA.WPF.Base.Interfaces.IAutoFocusVMFactory", "NINA.Joko.Plugins.HocusFocus.AutoFocus.HocusFocusVMFactory" }
        };

        public PluggableBehaviorSelector(IProfileService profileService, DefaultT ninaDefault) {
            this.profileService = profileService;
            this.ninaDefault = ninaDefault;
            Behaviors = new AsyncObservableCollection<T>();
            Behaviors.Add(ninaDefault);

            this.profileService.ProfileChanged += ProfileService_ProfileChanged;
            this.profileService.ActiveProfile.ApplicationSettings.PropertyChanged += ApplicationSettings_PropertyChanged;
            profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviorsLookup.TryGetValue(typeof(T).FullName, out selectedContentId);
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            this.profileService.ActiveProfile.ApplicationSettings.PropertyChanged += ApplicationSettings_PropertyChanged;
            DetectSelectedBehaviorChanged();
            PersistForcedSelection();
        }

        private void ApplicationSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors)) {
                DetectSelectedBehaviorChanged();
            }
        }

        private void DetectSelectedBehaviorChanged() {
            profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviorsLookup.TryGetValue(typeof(T).FullName, out string contentId);
            if (contentId != selectedContentId) {
                SelectedBehaviorChanged?.Invoke(this, new EventArgs());
                selectedContentId = contentId;
            }
        }

        public Type GetInterfaceType() {
            return typeof(T);
        }

        private void Behaviors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(Behaviors));
            RaisePropertyChanged(nameof(SelectedBehavior));
        }

        private AsyncObservableCollection<T> behaviors;

        public event EventHandler SelectedBehaviorChanged;

        public AsyncObservableCollection<T> Behaviors {
            get => behaviors;
            set {
                if (behaviors != value) {
                    if (behaviors != null) {
                        behaviors.CollectionChanged -= Behaviors_CollectionChanged;
                    }
                    behaviors = value;
                    if (behaviors != null) {
                        behaviors.CollectionChanged += Behaviors_CollectionChanged;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SelectedBehavior));
                }
            }
        }

        public T SelectedBehavior {
            get => GetBehavior();
            set {
                if (value == null) {
                    throw new ArgumentException("SelectedBehavior cannot be set to null", "SelectedBehavior");
                }
                if (!Behaviors.Any(b => b.ContentId == value.ContentId)) {
                    throw new ArgumentException($"{value.ContentId} is not a plugged {typeof(T).FullName} behavior", "SelectedBehavior");
                }

                var existingSelection = profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.FirstOrDefault(p => p.Key == typeof(T).FullName);
                if (!String.IsNullOrEmpty(existingSelection.Key)) {
                    profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.Remove(existingSelection);
                }
                profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>(typeof(T).FullName, value.ContentId));
                RaisePropertyChanged();
            }
        }

        public T GetBehavior(string pluggableBehaviorContentId) {
            if (String.IsNullOrEmpty(pluggableBehaviorContentId)) {
                return ninaDefault;
            }

            var selected = behaviors.FirstOrDefault(b => b.ContentId == pluggableBehaviorContentId);
            if (selected != null) {
                return selected;
            }
            return ninaDefault;
        }

        public T GetBehavior() {
            // Force HocusFocus for star detection, annotation, and autofocus
            var forced = GetForcedBehavior();
            if (forced != null) {
                return forced;
            }

            // Fallback to profile settings or default
            profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviorsLookup.TryGetValue(typeof(T).FullName, out string contentId);
            return GetBehavior(contentId);
        }

        /// <summary>The HocusFocus behavior forced for this interface, or null when none is forced or it is not loaded.</summary>
        private T GetForcedBehavior() {
            if (!ForcedHocusFocusBehaviors.TryGetValue(typeof(T).FullName, out var forcedContentId)) {
                return null;
            }
            return behaviors.FirstOrDefault(b => b.ContentId == forcedContentId);
        }

        /// <summary>
        /// Records the forced behavior as the profile's selection too. Code that reads the selection straight
        /// from the profile rather than through this selector (HocusFocus' star detection decides "is this
        /// NINA's own auto-focus?" that way) would otherwise see no selection, conclude the stock NINA
        /// auto-focuser runs, and apply NINA's auto-focus crop to HocusFocus runs.
        /// </summary>
        private void PersistForcedSelection() {
            var forced = GetForcedBehavior();
            if (forced == null) {
                return;
            }
            profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviorsLookup.TryGetValue(typeof(T).FullName, out string contentId);
            if (contentId != forced.ContentId) {
                Logger.Info($"Saving {forced.ContentId} as the profile's {typeof(T).Name} selection, as pins always uses it");
                SelectedBehavior = forced;
            }
        }

        public void AddBehavior(object behavior) {
            var typedBehavior = behavior as T;
            if (typedBehavior == null) {
                throw new ArgumentException($"Can't add behavior {behavior?.GetType().FullName ?? "<null>"} since it doesn't implement {typeof(T).FullName}", nameof(behavior));
            }

            Behaviors.Add(typedBehavior);
            PersistForcedSelection();
        }
    }
}
