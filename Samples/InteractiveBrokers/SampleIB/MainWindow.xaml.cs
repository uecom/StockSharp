#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIB.SampleIBPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIB
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.InteractiveBrokers;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly InteractiveBrokersTrader Trader = new InteractiveBrokersTrader();
		private bool _initialized;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly ConditionOrdersWindow _conditionOrdersWindow = new ConditionOrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Interactive Brokers");

			_ordersWindow.MakeHideable();
			_conditionOrdersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_newsWindow.MakeHideable();

			Trader.LogLevel = LogLevels.Debug;
			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener("logs.txt"));

			Tws.IsChecked = true;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_conditionOrdersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_conditionOrdersWindow.Close();
			_portfoliosWindow.Close();
			_newsWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isConnected)
				{
					if (!_initialized)
					{
						_initialized = true;

						// subscribe on connection successfully event
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));

							// subscribe on news
							Trader.RegisterNews();
						};

						// subscribe on connection error event
						Trader.ConnectionError += error => this.GuiAsync(() =>
						{
							ChangeConnectStatus(false);
							MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
						});

						Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

						// subscribe on error event
						Trader.Error += error =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

						// subscribe on error of market data subscription event
						Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

						Trader.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);
						Trader.NewMyTrade += trade => _myTradesWindow.TradeGrid.Trades.Add(trade);
						Trader.NewTrade += trade => _tradesWindow.TradeGrid.Trades.Add(trade);
						Trader.NewOrder += order => _ordersWindow.OrderGrid.Orders.Add(order);
						Trader.NewStopOrder += order => _conditionOrdersWindow.OrderGrid.Orders.Add(order);
						Trader.NewCandles += _securitiesWindow.AddCandles;

						Trader.NewPortfolio += portfolio =>
						{
							_portfoliosWindow.PortfolioGrid.Portfolios.Add(portfolio);
							Trader.RegisterPortfolio(portfolio);
						};
						Trader.NewPosition += position => _portfoliosWindow.PortfolioGrid.Positions.Add(position);

						// subscribe on error of order registration event
						Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
						// subscribe on error of order cancelling event
						Trader.OrderCancelFailed += OrderFailed;

						Trader.MassOrderCancelFailed += (transId, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

						Trader.NewNews += news => _newsWindow.NewsPanel.NewsGrid.News.Add(news);

						// set market data provider
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

						// set news provider
						_newsWindow.NewsPanel.NewsProvider = Trader;
					}

					Trader.Address = Address.Text.To<EndPoint>();
					Trader.Connect();
				}
				else
				{
					Trader.UnRegisterNews();

					Trader.Disconnect();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, LocalizedStrings.Str152);
			}
		}

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
			ConnectionStatus.Content = isConnected ? LocalizedStrings.Connected : LocalizedStrings.Disconnected;

			ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowNews.IsEnabled =
            ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
            ShowPortfolios.IsEnabled = isConnected;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowConditionOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_conditionOrdersWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void OnAddrTypeChecked(object sender, RoutedEventArgs e)
		{
			Address.Text = (Tws.IsChecked == true
				? InteractiveBrokersMessageAdapter.DefaultAddress
				: InteractiveBrokersMessageAdapter.DefaultGatewayAddress).To<string>();
		}
	}
}
