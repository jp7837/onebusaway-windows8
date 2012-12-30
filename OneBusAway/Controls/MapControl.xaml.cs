﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Bing.Maps;
using Windows.Devices.Geolocation;
using OneBusAway.Utilities;
using OneBusAway.Model;
using Windows.UI;
using OneBusAway.ViewModels;

namespace OneBusAway.Controls
{
    public partial class MapControl : UserControl
    {
        private UserLocationIcon userLocationIcon;
        private bool centerOnUserLocation;
        private Location userLocation;
        private HashSet<string> displayedBusStopLookup = new HashSet<string>();

        public MapControl()
        {
            this.InitializeComponent();

            this.userLocationIcon = new UserLocationIcon();

            map.ViewChangeEnded += OnMapViewChangeEnded;            
        }

        public string BingMapCredentials
        {
            get
            {
                return Utilities.UtilitiesConstants.BingMapCredentials;
            }
        }

        #region DependencyProperty backed CLR Properties

        public double BoundsWidth
        {
            get
            {
                return map.Bounds.Width;
            }
            set
            {
                map.Bounds.Width = value;
            }
        }

        public double BoundsHeight
        {
            get
            {
                return map.Bounds.Height;
            }
            set
            {
                map.Bounds.Height = value;
            }
        }

        public OneBusAway.Model.Point MapCenter 
        {
            get
            {
                return new OneBusAway.Model.Point()
                {
                    Latitude = map.Center.Latitude,
                    Longitude = map.Center.Longitude
                };
            }
            set
            {
                if (value != null)
                {
                    map.Center = new Location(value.Latitude, value.Longitude);

                    map.SetView(map.Center, ZoomLevel, new TimeSpan());
                }
                else
                {
                    map.Center = new Location(UtilitiesConstants.DefaultLatitude, UtilitiesConstants.DefaultLongitude);

                    map.SetView(map.Center, ZoomLevel, new TimeSpan());
                }
            }
        }

        public OneBusAway.Model.Point UserLocation
        {
            get
            {
                return GetValue(UserLocationDP) as OneBusAway.Model.Point;
            }
            set
            {
                SetValue(UserLocationDP, value);
            }
        }

        public bool CenterOnUserLocation
        {
            get
            {
                return centerOnUserLocation;
            }
            set
            {
                centerOnUserLocation = value;

                if (centerOnUserLocation && userLocation != null)
                {
                    map.SetView(userLocation, ZoomLevel);
                }
            }
        }

        public double ZoomLevel
        {
            get
            {
                return (double)GetValue(ZoomLevelDP);
            }
            set
            {
                SetValue(ZoomLevelDP, value);
            }
        }

        public List<Stop> BusStops
        {
            get
            {
                return GetValue(BusStopsDP) as List<Stop>;
            }
            set
            {
                SetValue(BusStopsDP, value);
            }
        }

        public List<Shape> Shapes
        {
            get
            {
                return GetValue(ShapesDP) as List<Shape>;
            }
            set
            {
                SetValue(ShapesDP, value);
            }
        }

        public MapView MapView
        {
            get
            {
                return GetValue(MapViewDP) as MapView;
            }
            set
            {
                SetValue(MapViewDP, value);
            }
        }

        public bool ClearBusStopsOnZoomOut
        {
            get
            {
                return (bool)GetValue(ClearBusStopsOnZoomOutDP);
            }
            set
            {
                SetValue(ClearBusStopsOnZoomOutDP, value);
            }
        }

        public BusStopControlViewModel SelectedBusStop
        {
            get
            {
                return GetValue(SelectedBusStopDP) as BusStopControlViewModel;
            }
            set
            {
                SetValue(SelectedBusStopDP, value);                
            }
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty MapCenterDP = DependencyProperty.Register("MapCenter", typeof(OneBusAway.Model.Point), typeof(MapControl), new PropertyMetadata(null, MapCenterPropertyChanged));

        public static readonly DependencyProperty UserLocationDP = DependencyProperty.Register("UserLocation", typeof(OneBusAway.Model.Point), typeof(MapControl), new PropertyMetadata(null, UserLocationChanged));

        public static readonly DependencyProperty CnterOnUserLocationDP = DependencyProperty.Register("CenterOnUserLocation", typeof(bool), typeof(MapControl), new PropertyMetadata(null, CenterOnUserLocationChanged));

        public static readonly DependencyProperty ZoomLevelDP = DependencyProperty.Register("ZoomLevel", typeof(double), typeof(MapControl), new PropertyMetadata(null, ZoomLevelChanged));

        public static readonly DependencyProperty BusStopsDP = DependencyProperty.Register("BusStops", typeof(OneBusAway.Model.Stop), typeof(MapControl), new PropertyMetadata(null, BusStopsChanged));

        public static readonly DependencyProperty BoundsWidthDP = DependencyProperty.Register("BoundsWidth", typeof(double), typeof(MapControl), null);

        public static readonly DependencyProperty BoundsHeightDP = DependencyProperty.Register("BoundsHeight", typeof(double), typeof(MapControl), null);

        public static readonly DependencyProperty MapViewDP = DependencyProperty.Register("MapView", typeof(MapView), typeof(MapControl), new PropertyMetadata(null, MapViewChanged));

        public static readonly DependencyProperty ShapesDP = DependencyProperty.Register("Shapes", typeof(OneBusAway.Model.Shape), typeof(MapControl), new PropertyMetadata(null, ShapesChanged));

        public static readonly DependencyProperty ClearBusStopsOnZoomOutDP = DependencyProperty.Register("ClearBusStopsOnZoomOut", typeof(bool), typeof(MapControl), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedBusStopDP = DependencyProperty.Register("SelectedBusStop", typeof(BusStopControlViewModel), typeof(MapControl), new PropertyMetadata(null, new PropertyChangedCallback(SelectedBusStopChanged)));

        #endregion

        #region Dependency Property Changed Callbacks

        private static void MapViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapControl = d as MapControl;
            var newValue = e.NewValue as MapView;

            if (e.OldValue == null && newValue != null)
            {
                if (newValue.MapCenter.Latitude != mapControl.map.Center.Latitude ||
                    newValue.MapCenter.Longitude != mapControl.map.Center.Longitude)
                {
                    mapControl.map.Center = new Location(newValue.MapCenter.Latitude, newValue.MapCenter.Longitude);
                }

                if (newValue.ZoomLevel != mapControl.map.ZoomLevel)
                {
                    mapControl.map.ZoomLevel = newValue.ZoomLevel;
                }
            }
        }

        /// <summary>
        /// Change the polylines on the map.
        /// </summary>
        private static void ShapesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MapControl mapControl = (MapControl)d;

            if (e.OldValue != null)
            {
                // remove all old polylines from the map:
                mapControl.map.ShapeLayers.Clear();
            }

            if (e.NewValue != null)
            {
                MapShapeLayer routeLayer = new MapShapeLayer();                
                
                List<Shape> shapes = (List<Shape>)e.NewValue;
                foreach (var shape in shapes)
                {
                    MapPolyline polyline = new MapPolyline();
                    polyline.Color = Color.FromArgb(255, 0x78, 0xAA, 0x36);
                    polyline.Width = 6;

                    foreach (var point in shape.Points)
                    {
                        polyline.Locations.Add(new Location(point.Latitude, point.Longitude));
                    }

                    routeLayer.Shapes.Add(polyline);
                }

                mapControl.map.ShapeLayers.Add(routeLayer);
            }
        }

        private static void SelectedBusStopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BusStopControlViewModel lastSelected = e.OldValue as BusStopControlViewModel;
            if (lastSelected != null)
            {
                lastSelected.IsSelected = false;
            }

            BusStopControlViewModel newSelected = e.NewValue as BusStopControlViewModel;
            if (newSelected != null)
            {
                newSelected.IsSelected = true;

                // Make sure the new view model is bound to an existing control.
                // If it's not then we need to find the one that is and make it selected:
                MapControl mapControl = (MapControl)d;
                var boundViewModel = (from busStop in mapControl.map.Children
                                      let busStopControl = busStop as BusStop
                                      where busStopControl != null
                                      let busStopControlViewModel = busStopControl.DataContext as BusStopControlViewModel
                                      where busStopControlViewModel != null
                                        && string.Equals(busStopControlViewModel.StopId, newSelected.StopId, StringComparison.OrdinalIgnoreCase)
                                        && busStopControlViewModel != newSelected
                                      select busStopControlViewModel).FirstOrDefault();

                // This means we have a control that matches the selected control, but it is not
                // the same view model.
                if (boundViewModel != null)
                {
                    mapControl.SelectedBusStop = boundViewModel;
                }
            }
        }

        private static void MapCenterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {            
            var mapControl = d as MapControl;

            mapControl.MapCenter = (OneBusAway.Model.Point)e.NewValue;
        }        

        private static void UserLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapControl = d as MapControl;
            var newValue = e.NewValue as OneBusAway.Model.Point;

            if (newValue != null)
            {
                mapControl.userLocation = new Location(newValue.Latitude, newValue.Longitude);

                if (!mapControl.map.Children.Contains(mapControl.userLocationIcon))
                {
                    mapControl.map.Children.Add(mapControl.userLocationIcon);
                }

                MapLayer.SetPosition(mapControl.userLocationIcon, mapControl.userLocation);
            }
            else
            {
                mapControl.map.Children.Remove(mapControl.userLocationIcon);
            }
        }

        private static void CenterOnUserLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapControl = d as MapControl;

            mapControl.CenterOnUserLocation = (bool)e.NewValue;
        }

        private static void ZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapControl = d as MapControl;
            var zoomLevel = (double)e.NewValue;

            if (mapControl.map.ZoomLevel != zoomLevel)
            {
                mapControl.map.ZoomLevel = zoomLevel;
            }
        }

        private static void BusStopsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapControl = d as MapControl;

            if (e.NewValue == null)
            {
                mapControl.displayedBusStopLookup.Clear();
                mapControl.map.Children.Clear();

                mapControl.map.Children.Add(mapControl.userLocationIcon);
                MapLayer.SetPosition(mapControl.userLocationIcon, mapControl.userLocation);
            }
            else
            {
                MapControlViewModel mapControlViewModel = (MapControlViewModel)mapControl.DataContext;
                var stops = e.NewValue as List<Stop>;
                foreach (var stop in stops)
                {
                    // If we're not already displaying this bus stop then add it to the list:
                    if (!mapControl.displayedBusStopLookup.Contains(stop.StopId))
                    {
                        BusStopControlViewModel busStopControlViewModel = new BusStopControlViewModel(mapControlViewModel, stop);

                        if (mapControlViewModel.SelectedBusStop != null)
                        {
                            busStopControlViewModel.IsSelected = string.Equals(mapControlViewModel.SelectedBusStop.StopId, stop.StopId, StringComparison.OrdinalIgnoreCase);
                            if (busStopControlViewModel.IsSelected)
                            {
                                mapControlViewModel.SelectedBusStop = busStopControlViewModel;
                            }
                        }

                        BusStop busStopIcon = new BusStop()
                        {
                            DataContext = busStopControlViewModel
                        };

                        mapControl.map.Children.Add(busStopIcon);
                        mapControl.displayedBusStopLookup.Add(stop.StopId);

                        MapLayer.SetPosition(busStopIcon, new Location(stop.Latitude, stop.Longitude));
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Called when the map view change ends.  Store the map view and if we zoom out far enough, 
        /// clear the bus stops.
        /// </summary>
        void OnMapViewChangeEnded(object sender, ViewChangeEndedEventArgs e)
        {
            this.MapView = new MapView(new Model.Point(map.Center.Latitude, map.Center.Longitude), 
                map.ZoomLevel, 
                map.Bounds.Height, 
                map.Bounds.Width);
        }         
    }
}