using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CustomControlLibrary
{

    /// <summary>
    /// Currency converter control.
    /// </summary>
    [TemplatePart(Name = "PART_CurrencyFrom", Type = typeof(ComboBox))]
    [TemplatePart(Name = "PART_CurrencyTo", Type = typeof(ComboBox))]
    [TemplatePart(Name = "PART_CurrencyFromValue", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_CurrencyToValue", Type = typeof(TextBox))]
    [TemplateVisualState(Name = "SelectorsReady", GroupName = "SelectorsStates")]
    [TemplateVisualState(Name = "SelectorsReady", GroupName = "SelectorsStates")]
    [TemplateVisualState(Name = "InputReady", GroupName = "InputStates")]
    [TemplateVisualState(Name = "InputLoading", GroupName = "InputStates")]
    public class CurrencyConverterControl : Control
    {
        #region Provider
        /// <summary>
        /// Setup exchange rate provider.
        /// </summary>
        public IExchangeRateProvider ExchangeRateProvider
        {
            get => _exchangeRateProvider;
            set
            {
                _exchangeRateProvider = value;
                TryInvalidateExchangeRate();
            }
        }
        #endregion

        #region Currency

        /// <summary>
        /// Selected currency id for conversion from.
        /// </summary>
        public string SelectedCurrencyIdFrom
        {
            get { return (string)GetValue(SelectedCurrencyIdFromProperty); }
            set { SetValue(SelectedCurrencyIdFromProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCurrencyIdFrom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCurrencyIdFromProperty =
            DependencyProperty.Register(nameof(SelectedCurrencyIdFrom), typeof(string), typeof(CurrencyConverterControl), new PropertyMetadata("USD", OnSelectedCurrencyIdFromChanged));

        private static void OnSelectedCurrencyIdFromChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as CurrencyConverterControl;
            if (!instance._uiInited)
            {
                return;
            }

            instance.InvalidateCurrencyComponentView(instance.CurrencyFrom, instance.SelectedCurrencyIdFrom);
            instance.TryInvalidateExchangeRate();
        }

        /// <summary>
        /// Selected currency id for conversion to.
        /// </summary>
        public string SelectedCurrencyIdTo
        {
            get { return (string)GetValue(SelectedCurrencyIdToProperty); }
            set { SetValue(SelectedCurrencyIdToProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCurrencyIdTo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCurrencyIdToProperty =
            DependencyProperty.Register(nameof(SelectedCurrencyIdTo), typeof(string), typeof(CurrencyConverterControl), new PropertyMetadata("EUR", OnSelectedCurrencyIdToChanged));

        private static void OnSelectedCurrencyIdToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as CurrencyConverterControl;
            if (!instance._uiInited)
            {
                return;
            }

            instance.InvalidateCurrencyComponentView(instance.CurrencyTo, instance.SelectedCurrencyIdTo);
            instance.TryInvalidateExchangeRate();
        }

        /// <summary>
        /// Amount of currency conversion from.
        /// </summary>
        public double CurrencyFromAmount
        {
            get { return (double)GetValue(CurrencyFromAmountProperty); }
            set { SetValue(CurrencyFromAmountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrencyFromAmount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrencyFromAmountProperty =
            DependencyProperty.Register(nameof(CurrencyFromAmount), typeof(double), typeof(CurrencyConverterControl), new PropertyMetadata(1.0, OnCurrencyFromAmountChanged));

        private static void OnCurrencyFromAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inst = d as CurrencyConverterControl;
            if (inst._preventCycleCalculation || double.IsNaN(inst.Rate))
            {
                return;
            }

            inst.InvalidateConvertion();
        }

        /// <summary>
        /// Amount of currency conversion to.
        /// </summary>
        public double CurrencyToAmount
        {
            get { return (double)GetValue(CurrencyToAmountProperty); }
            set { SetValue(CurrencyToAmountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrencyToAmount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrencyToAmountProperty =
            DependencyProperty.Register(nameof(CurrencyToAmount), typeof(double), typeof(CurrencyConverterControl), new PropertyMetadata(0.0, OnCurrencyToAmountChanged));

        private static void OnCurrencyToAmountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inst = d as CurrencyConverterControl;
            if (inst._preventCycleCalculation || double.IsNaN(inst.Rate))
            {
                return;
            }

            inst.InvalidateConvertion(true);
        }

        #endregion

        #region Rate
        private static readonly DependencyPropertyKey RateKey = DependencyProperty.RegisterReadOnly(
          "Rate",
          typeof(double),
          typeof(CurrencyConverterControl),
          new PropertyMetadata(double.NaN)
        );

        public static readonly DependencyProperty RateProperty = RateKey.DependencyProperty;

        /// <summary>
        /// Exchange rate.
        /// </summary>
        public double Rate
        {
            get { return (double)GetValue(RateProperty); }
        }
        #endregion

        #region CountriesItems

        private static DependencyPropertyKey CounriesItemsKey = DependencyProperty.RegisterReadOnly(
            nameof(CounriesItems),
            typeof(List<CountryInfo>), 
            typeof(CurrencyConverterControl), 
            new PropertyMetadata(new List<CountryInfo>()));

        public static DependencyProperty CounriesItemsProperty = CounriesItemsKey.DependencyProperty;
        
        /// <summary>
        /// List of countries.
        /// </summary>
        public List<CountryInfo> CounriesItems
        {
            get { return (List<CountryInfo>)GetValue(CounriesItemsProperty); }
        }

        #endregion

        #region View
        private ComboBox _currencyFrom;
        private ComboBox CurrencyFrom
        {
            get => _currencyFrom;
            set
            {
                if (_currencyFrom != null)
                {
                    _currencyFrom.SelectionChanged -= CurrencyFromOnSelectionChanged;
                    BindingOperations.ClearAllBindings(_currencyFrom);
                    _currencyFrom = null;
                }

                if (value == null)
                {
                    return;
                }

                _currencyFrom = value;
                var bind = new Binding(nameof(CounriesItems))
                {
                    Source = this
                };
                _currencyFrom.SetBinding(ComboBox.ItemsSourceProperty, bind);

                InvalidateCurrencyComponentView(_currencyFrom, SelectedCurrencyIdFrom);
                _currencyFrom.SelectionChanged += CurrencyFromOnSelectionChanged;
            }
        }

        private TextBox _currencyFromValue;
        private TextBox CurrencyFromValue
        {
            get => _currencyFromValue;
            set
            {
                if (_currencyFromValue != null)
                {
                    BindingOperations.ClearAllBindings(_currencyFromValue);
                    _currencyFromValue = null;
                }

                if (value == null)
                {
                    return;
                }

                _currencyFromValue = value;
                var bind = new Binding(nameof(CurrencyFromAmount))
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new NumberToStringConvertor(),
                    Delay = 350
                };
                _currencyFromValue.SetBinding(TextBox.TextProperty, bind);
            }
        }

        private ComboBox _currencyTo;
        private ComboBox CurrencyTo
        {
            get => _currencyTo;
            set
            {
                if (_currencyTo != null)
                {
                    _currencyTo.SelectionChanged -= CurrencyToOnSelectionChanged;
                    BindingOperations.ClearAllBindings(_currencyTo);
                    _currencyTo = null;
                }
                
                if (value == null)
                {
                    return;
                }

                _currencyTo = value;
                var bind = new Binding(nameof(CounriesItems))
                {
                    Source = this
                };
                _currencyTo.SetBinding(ComboBox.ItemsSourceProperty, bind);

                InvalidateCurrencyComponentView(_currencyTo, SelectedCurrencyIdTo);
                _currencyTo.SelectionChanged += CurrencyToOnSelectionChanged;
            }
        }

        private TextBox _currencyToValue;
        private TextBox CurrencyToValue
        {
            get => _currencyToValue;
            set
            {
                if (_currencyToValue != null)
                {
                    BindingOperations.ClearAllBindings(_currencyToValue);
                    _currencyToValue = null;
                }

                if (value == null)
                {
                    return;
                }
                _currencyToValue = value;
                var bind = new Binding(nameof(CurrencyToAmount))
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new NumberToStringConvertor(),
                    Delay = 350
                };
                _currencyToValue.SetBinding(TextBox.TextProperty, bind);
            }
        }

        private void CurrencyFromOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = CurrencyFrom.SelectedItem as CountryInfo;
            if(selectedItem==null)
            {
                return;
            }
            SelectedCurrencyIdFrom = selectedItem.CurrencyId;
        }

        private void CurrencyToOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = CurrencyTo.SelectedItem as CountryInfo;
            if (selectedItem == null)
            {
                return;
            }

            SelectedCurrencyIdTo = selectedItem.CurrencyId;
        }

        #endregion

        private bool _preventCycleCalculation;
        private IExchangeRateProvider _exchangeRateProvider;
        private CancellationTokenSource _cancellationTokenSource;

        static CurrencyConverterControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CurrencyConverterControl), new FrameworkPropertyMetadata(typeof(CurrencyConverterControl)));
        }

        public CurrencyConverterControl()
        {
            InitializeModel();
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;

            FinalizeUI();
            FinalizeModel();
        }

        public override void OnApplyTemplate()
        {
            InitializeUI();
            TryInvalidateExchangeRate();
            base.OnApplyTemplate();
        }

        private void InitializeModel()
        {
            var resource = Application.GetResourceStream(new Uri("/CustomControlLibrary;component/assets/countries.json", UriKind.Relative));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<CountryInfo>));
            var countries = (ser.ReadObject(resource.Stream) as List<CountryInfo>);
 
            SetValue(CounriesItemsKey, countries);
        }

        private void FinalizeModel()
        {
            CounriesItems.Clear();
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private bool _uiInited;
        private void InitializeUI()
        {
            CurrencyFrom = (ComboBox)GetTemplateChild("PART_CurrencyFrom");
            CurrencyTo = (ComboBox)GetTemplateChild("PART_CurrencyTo");
            CurrencyFromValue = (TextBox)GetTemplateChild("PART_CurrencyFromValue") ;
            CurrencyToValue = (TextBox)GetTemplateChild("PART_CurrencyToValue");

            _uiInited = true;
        }

        private void FinalizeUI()
        {
            CurrencyFrom = null;
            CurrencyTo = null;
            CurrencyFromValue = null;
            CurrencyToValue = null;
            _uiInited = false;
        }

        private void InvalidateCurrencyComponentView(ComboBox currencyViewComponent, string currencyId)
        {
            var si = currencyViewComponent.SelectedItem as CountryInfo;
            if (si!=null && si.CurrencyId.Equals(currencyId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            currencyViewComponent.SelectedIndex = CounriesItems.FindIndex(item => item.CurrencyId.Equals(currencyId, StringComparison.OrdinalIgnoreCase));
        }

        private async void TryInvalidateExchangeRate()
        {
            VisualStateManager.GoToState(this, "SelectorsLoading", true);
            VisualStateManager.GoToState(this, "InputLoading", true);
            if (_exchangeRateProvider == null)
            {
                // Leave control at loading state while waiting setting of provider.
                return;
            }
            var rate = await InvalidateExchangeRate();
            if (double.IsNaN(rate))
            {
                // In case of provider return invalid value enabled control to select other currencies of conversion.
                VisualStateManager.GoToState(this, "SelectorsReady", true);
                return;
            }
            // Got an exchange rate.
            SetValue(RateKey, rate);
            InvalidateConvertion();
            VisualStateManager.GoToState(this, "SelectorsReady", true);
            VisualStateManager.GoToState(this, "InputReady", true);
        }

        private async Task<double> InvalidateExchangeRate()
        {
            if (_cancellationTokenSource != null/* && !_cancellationTokenSource.IsCancellationRequested*/)
            {
                // Cancel any previous requests to provider.
                _cancellationTokenSource.Cancel();
            }

            using (_cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var token = _cancellationTokenSource.Token;
                    var fromCurrency = SelectedCurrencyIdFrom;
                    var toCurrency = SelectedCurrencyIdTo;
                    var rate = await Task.Run(() => _exchangeRateProvider.GetExchangeRate(fromCurrency, toCurrency, token), token);
                    _cancellationTokenSource = null;
                    return rate;
                    
                }
                catch (Exception ex) when (ex is TaskCanceledException
                        || (ex is AggregateException && ex.InnerException is TaskCanceledException))
                {
                    // Do nothing
                }
                _cancellationTokenSource = null;
                return double.NaN;
            }
        }

        private void InvalidateConvertion(bool convertBack = false)
        {
            _preventCycleCalculation = true;
            if (convertBack)
            {
                CurrencyFromAmount = Math.Round(CurrencyToAmount * 1 / Rate, 2);
            }
            else
            {
                CurrencyToAmount = Math.Round(CurrencyFromAmount * Rate, 2);
            }
            _preventCycleCalculation = false;
        }
    }

    /// <summary>
    /// Country info.
    /// </summary>
    [DataContract]
    public class CountryInfo
    {
        /// <summary>
        /// Country id (iso3 format).
        /// </summary>
        [DataMember(Name = "iso3")]
        public string Id { get; set; }
        
        /// <summary>
        /// Currency id.
        /// </summary>
        [DataMember(Name = "currency")]
        public string CurrencyId { get; set; }

        /// <summary>
        /// Country id (iso2 format).
        /// </summary>
        [DataMember(Name = "iso2")]
        public string Iso2Id { get; set; }
        
        /// <summary>
        /// Flag asset <see cref="Uri"/>.
        /// </summary>
        public Uri Flag { get; set; }

        /// <summary>
        /// Called immediately after deserialization of an object.
        /// </summary>
        /// <param name="context"><see cref="StreamingContext"/>parameter</param>
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context) => Flag = new Uri($"/CustomControlLibrary;component/assets/flags/{Iso2Id}.png", UriKind.Relative);

    }

}