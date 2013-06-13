﻿using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gemini.Modules.GraphEditor.Controls
{
    // Inspired by studying http://www.codeproject.com/Articles/182683/NetworkView-A-WPF-custom-control-for-visualizing-a
    // Thank you Ashley Davis!
    public class GraphControl : Control
    {
        private ElementItemsControl _elementItemsControl = null;

        static GraphControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphControl),
                new FrameworkPropertyMetadata(typeof(GraphControl)));
        }

        #region Dependency properties

        public static readonly DependencyProperty ElementsSourceProperty = DependencyProperty.Register(
            "ElementsSource", typeof(IEnumerable), typeof(GraphControl));

        public IEnumerable ElementsSource
        {
            get { return (IEnumerable) GetValue(ElementsSourceProperty); }
            set { SetValue(ElementsSourceProperty, value); }
        }

        public static readonly DependencyProperty ElementItemContainerStyleProperty = DependencyProperty.Register(
            "ElementItemContainerStyle", typeof(Style), typeof(GraphControl));

        public Style ElementItemContainerStyle
        {
            get { return (Style) GetValue(ElementItemContainerStyleProperty); }
            set { SetValue(ElementItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty ElementItemTemplateProperty = DependencyProperty.Register(
            "ElementItemTemplate", typeof(DataTemplate), typeof(GraphControl));

        public DataTemplate ElementItemTemplate
        {
            get { return (DataTemplate) GetValue(ElementItemTemplateProperty); }
            set { SetValue(ElementItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ConnectionsSourceProperty = DependencyProperty.Register(
            "ConnectionsSource", typeof(IEnumerable), typeof(GraphControl));

        public IEnumerable ConnectionsSource
        {
            get { return (IEnumerable) GetValue(ConnectionsSourceProperty); }
            set { SetValue(ConnectionsSourceProperty, value); }
        }

        public static readonly DependencyProperty ConnectionItemContainerStyleProperty = DependencyProperty.Register(
            "ConnectionItemContainerStyle", typeof(Style), typeof(GraphControl));

        public Style ConnectionItemContainerStyle
        {
            get { return (Style) GetValue(ConnectionItemContainerStyleProperty); }
            set { SetValue(ConnectionItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty ConnectionItemTemplateProperty = DependencyProperty.Register(
            "ConnectionItemTemplate", typeof(DataTemplate), typeof(GraphControl));

        public DataTemplate ConnectionItemTemplate
        {
            get { return (DataTemplate) GetValue(ConnectionItemTemplateProperty); }
            set { SetValue(ConnectionItemTemplateProperty, value); }
        }

        #endregion

        #region Routed events

        public static readonly RoutedEvent ConnectionDragStartedEvent = EventManager.RegisterRoutedEvent(
            "ConnectionDragStarted", RoutingStrategy.Bubble, typeof(ConnectionDragStartedEventHandler), 
            typeof(GraphControl));

        public static readonly RoutedEvent QueryConnectionFeedbackEvent = EventManager.RegisterRoutedEvent(
            "QueryConnectionFeedback", RoutingStrategy.Bubble, typeof(QueryConnectionFeedbackEventHandler), 
            typeof(GraphControl));

        public static readonly RoutedEvent ConnectionDraggingEvent = EventManager.RegisterRoutedEvent(
            "ConnectionDragging", RoutingStrategy.Bubble, typeof(ConnectionDraggingEventHandler), 
            typeof(GraphControl));

        public static readonly RoutedEvent ConnectionDragCompletedEvent = EventManager.RegisterRoutedEvent(
            "ConnectionDragCompleted", RoutingStrategy.Bubble, typeof(ConnectionDragCompletedEventHandler), 
            typeof(GraphControl));

        public event ConnectionDragStartedEventHandler ConnectionDragStarted
        {
            add { AddHandler(ConnectionDragStartedEvent, value); }
            remove { RemoveHandler(ConnectionDragStartedEvent, value); }
        }

        public event QueryConnectionFeedbackEventHandler QueryConnectionFeedback
        {
            add { AddHandler(QueryConnectionFeedbackEvent, value); }
            remove { RemoveHandler(QueryConnectionFeedbackEvent, value); }
        }

        public event ConnectionDraggingEventHandler ConnectionDragging
        {
            add { AddHandler(ConnectionDraggingEvent, value); }
            remove { RemoveHandler(ConnectionDraggingEvent, value); }
        }

        public event ConnectionDragCompletedEventHandler ConnectionDragCompleted
        {
            add { AddHandler(ConnectionDragCompletedEvent, value); }
            remove { RemoveHandler(ConnectionDragCompletedEvent, value); }
        }

        #endregion

        public IList SelectedElements
        {
            get { return _elementItemsControl.SelectedItems; }
        }

        public GraphControl()
        {
            AddHandler(ConnectorItem.ConnectorDragStartedEvent, new ConnectorItemDragStartedEventHandler(OnConnectorItemDragStarted));
            AddHandler(ConnectorItem.ConnectorDraggingEvent, new ConnectorItemDraggingEventHandler(OnConnectorItemDragging));
            AddHandler(ConnectorItem.ConnectorDragCompletedEvent, new ConnectorItemDragCompletedEventHandler(OnConnectorItemDragCompleted));
        }

        public override void OnApplyTemplate()
        {
            _elementItemsControl = (ElementItemsControl) Template.FindName("PART_ElementItemsControl", this);
            base.OnApplyTemplate();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _elementItemsControl.SelectedItems.Clear();
            base.OnMouseLeftButtonDown(e);
        }

        internal int GetMaxZIndex()
        {
            return _elementItemsControl.Items.Cast<object>()
                .Select(item => (ElementItem) _elementItemsControl.ItemContainerGenerator.ContainerFromItem(item))
                .Select(elementItem => elementItem.ZIndex)
                .Concat(new[] { 0 })
                .Max();
        }

        #region Connection dragging

        private ConnectorItem _sourceConnector;

        private void OnConnectorItemDragStarted(object sender, ConnectorItemDragStartedEventArgs e)
        {
            e.Handled = true;

            _sourceConnector = (ConnectorItem) e.OriginalSource;
            var elementItem = _sourceConnector.ParentElementItem;


        }

        private void OnConnectorItemDragging(object sender, ConnectorItemDraggingEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void OnConnectorItemDragCompleted(object sender, ConnectorItemDragCompletedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}