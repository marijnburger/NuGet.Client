// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft;
using Microsoft.VisualStudio.PlatformUI;
using Resx = NuGet.PackageManagement.UI.Resources;

namespace NuGet.PackageManagement.UI
{
    /// <summary>
    /// The panel which is located at the top of the package manager window.
    /// </summary>
    public partial class PackageManagerTopPanel : UserControl
    {
        private TabItem _selectedTabItem
        {
            get
            {
                return tabsPackageManagement.SelectedItem as TabItem;
            }
            set
            {
                tabsPackageManagement.SelectedItem = value;
            }
        }
        public TabItem _tabConsolidate { get; private set; }
        public Border _countConsolidateContainer { get; private set; }
        public TextBlock _countConsolidate { get; private set; }

        public PackageManagerTopPanel()
        {
            InitializeComponent();
        }

        public void CreateAndAddConsolidateTab(bool isSolution)
        {
            IsSolution = isSolution;
            if (IsSolution)
            {
                TabItem tabConsolidate = new TabItem();
                tabConsolidate.Name = nameof(tabConsolidate);
                tabConsolidate.Tag = "Consolidate"; //From ItemFilter Enum.


                StackPanel sp = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };

                TextBlock textConsolidate = new TextBlock()
                {
                    Name = nameof(textConsolidate),
                    Text = Resx.Action_Consolidate
                };
                sp.Children.Add(textConsolidate);

                Binding binding = new Binding();
                binding.Path = new PropertyPath(Resx.Action_Consolidate);

                //MultiBinding consolidateAutomationPropertiesName = new MultiBinding();
                //consolidateAutomationPropertiesName.StringFormat = " {0}{1}";
                //consolidateAutomationPropertiesName.Bindings.Add(binding);
                //consolidateAutomationPropertiesName.Bindings.Add(new Binding("Text") { Source = _countConsolidate });
                //tabConsolidate.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, consolidateAutomationPropertiesName);

                //tabConsolidate.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty, binding);

                SetConsolidationAutomationProperties(tabConsolidate, 0);

                //The textblock that displays the count.
                _countConsolidateContainer = new Border()
                {
                    Name = nameof(_countConsolidateContainer),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(3, 0, 3, 0),
                    Padding = new Thickness(3, 0, 3, 0),
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _countConsolidateContainer.SetResourceReference(Border.BackgroundProperty, Brushes.TabPopupBrushKey);
                sp.Children.Add(_countConsolidateContainer);

                _countConsolidate = new TextBlock()
                {
                    Name = nameof(_countConsolidate),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top
                };
                _countConsolidate.SetResourceReference(TextBlock.ForegroundProperty, Brushes.TabPopupTextBrushKey);
                _countConsolidateContainer.Child = _countConsolidate;

                tabConsolidate.Header = sp;
                _tabConsolidate = tabConsolidate;
                tabsPackageManagement.Items.Add(tabConsolidate);
            }
        }

        private void SetConsolidationAutomationProperties(TabItem tabConsolidate, int count)
        {
            string automationString = null;
            if (count > 0)
            {
                automationString = string.Format($" {Resx.Action_Consolidate}{count}");
            }
            else
            {
                automationString = Resx.Action_Consolidate;
            }
            tabConsolidate.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, automationString);
        }

        public void ShowWarningOnInstalledTab(int installedDeprecatedPackagesCount)
        {
            bool hasInstalledDeprecatedPackages = installedDeprecatedPackagesCount > 0;
            if (hasInstalledDeprecatedPackages)
            {
                _warningIcon.Visibility = Visibility.Visible;
                _warningIcon.ToolTip = string.Format(
                        CultureInfo.CurrentCulture,
                        NuGet.PackageManagement.UI.Resources.Label_Installed_DeprecatedWarning,
                        installedDeprecatedPackagesCount);
            }
            else
            {
                _warningIcon.Visibility = Visibility.Collapsed;
                _warningIcon.ToolTip = null;
            }
        }

        public void ShowCountOnConsolidateTab(int count)
        {
            if (count > 0)
            {
                _countConsolidate.Text = count.ToString(CultureInfo.CurrentCulture);
                _countConsolidateContainer.Visibility = Visibility.Visible;
            }
            else
            {
                _countConsolidateContainer.Visibility = Visibility.Collapsed;
            }
            SetConsolidationAutomationProperties(_tabConsolidate, count);
        }

        public void ShowCountOnUpdatesTab(int count)
        {
            if (count > 0)
            {
                _countUpdates.Text = count.ToString(CultureInfo.CurrentCulture);
                _countUpdatesContainer.Visibility = Visibility.Visible;
            }
            else
            {
                _countUpdatesContainer.Visibility = Visibility.Collapsed;
            }
        }

        // the control that is used as container for the search box.
        public Border SearchControlParent => _searchControlParent;

        public CheckBox CheckboxPrerelease => _checkboxPrerelease;

        public ComboBox SourceRepoList => _sourceRepoList;

        public ToolTip SourceToolTip => _sourceTooltip;

        public ItemFilter Filter => GetItemFilter(_selectedTabItem);

        private ItemFilter GetItemFilter(TabItem tabItem)
        {
            Assumes.Present(tabItem);
            return (ItemFilter)Enum.Parse(typeof(ItemFilter), tabItem.Tag as string);
        }

        public string Title
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        // Indicates if the control is hosted in solution package manager.
        private bool _isSolution;

        public bool IsSolution
        {
            get
            {
                return _isSolution;
            }
            set
            {
                if (_isSolution == value)
                {
                    return;
                }

                _isSolution = value;
                if (!_isSolution)
                {
                    // if consolidate tab is currently selected, we need to select another
                    // tab.
                    if (_selectedTabItem == _tabConsolidate)
                    {
                        SelectFilter(ItemFilter.Installed);
                    }
                }
            }
        }

        private void _checkboxPrerelease_Checked(object sender, RoutedEventArgs e)
        {
            PrereleaseCheckChanged?.Invoke(this, EventArgs.Empty);
        }

        private void _checkboxPrerelease_Unchecked(object sender, RoutedEventArgs e)
        {
            PrereleaseCheckChanged?.Invoke(this, EventArgs.Empty);
        }

        private void _sourceRepoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SourceRepoListSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void _settingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public event EventHandler<EventArgs> SettingsButtonClicked;

        public event EventHandler<EventArgs> PrereleaseCheckChanged;

        public event EventHandler<EventArgs> SourceRepoListSelectionChanged;

        public void SelectFilter(ItemFilter selectedFilter)
        {
            switch (selectedFilter)
            {
                case ItemFilter.All:
                    _selectedTabItem = tabBrowse;
                    break;

                case ItemFilter.Installed:
                    _selectedTabItem = tabInstalled;
                    break;

                case ItemFilter.UpdatesAvailable:
                    _selectedTabItem = tabUpdates;
                    break;

                case ItemFilter.Consolidate:
                    if (_isSolution)
                    {
                        _selectedTabItem = _tabConsolidate;
                    }
                    break;
            }

            // _selectedFilter could be null if we are running with a solution with user
            // settings saved by a later version of NuGet that has more filters than
            // can be recognized here.
            if (_selectedTabItem == null)
            {
                _selectedTabItem = tabInstalled;
            }
        }

        private void TabsPackageManagement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem previousTabItem = e.RemovedItems.Count > 0 ? e.RemovedItems[0] as TabItem : null;
            //TabItem selectedTabItem = e.AddedItems.Count > 0 ? e.AddedItems[0] as TabItem : null;

            if (previousTabItem != null)
            {
                ItemFilter previousFilter = GetItemFilter(previousTabItem);
                //_selectedTabItem = selectedTabItem; //TODO: this is redundant?

                if (FilterChanged != null)
                {
                    FilterChanged(this, new FilterChangedEventArgs(previousFilter));
                }
            }
        }
    }

    public class FilterChangedEventArgs : EventArgs
    {
        public ItemFilter? PreviousFilter
        {
            get;
        }

        public FilterChangedEventArgs(ItemFilter? previousFilter)
        {
            PreviousFilter = previousFilter;
        }
    }
}
