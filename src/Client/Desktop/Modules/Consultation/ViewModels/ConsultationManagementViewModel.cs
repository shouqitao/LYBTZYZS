using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Services.ErrorHandling;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{

    /// <summary>
    /// ���Ƽ�¼������ͼģ�� - �򻯰�
    /// ֻ������ʾ�ͻ����������Ƽ�¼�����������ӵ����̿���
    /// </summary>
    public class ConsultationManagementViewModel : UnifiedViewModelBase
    {

        #region ��������

        private readonly IConsultationService _consultationService;

        #endregion ��������

        #region ����

        private ObservableCollection<ConsultationDto> _consultations = new();

        public ObservableCollection<ConsultationDto> Consultations
        {
            get => _consultations;
            set => SetProperty(ref _consultations, value);
        }

        private ConsultationDto? _selectedConsultation;

        public ConsultationDto? SelectedConsultation
        {
            get => _selectedConsultation;
            set => SetProperty(ref _selectedConsultation, value);
        }

        private bool _isLoading = false;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _searchKeyword = string.Empty;

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        #endregion ����

        #region ����

        public ICommand LoadDataCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        #endregion ����

        #region ���캯��

        public ConsultationManagementViewModel(
        IConsultationService consultationService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager sessionManager)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand(ViewDetails, () => SelectedConsultation != null)
            .ObservesProperty(() => SelectedConsultation);

            // �޸�: ʹ��Task.Run��ȫ��ʼ������ֹδ�����쳣
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion ���캯��

        #region ��ʼ��

        private async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "��ʼ�����ƹ���ʧ��");

                // �ṩ�û��ѺõĴ�����ʾ
                ShowErrorMessage("���ƹ���ģ���ʼ��ʧ�ܣ��볢��ˢ��ҳ��");
            }
        }

        #endregion ��ʼ��

        #region ���ݲ���

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var query = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = SearchKeyword
                };

                var result = await _consultationService.GetPagedAsync(query.PageIndex, query.PageSize, query.Keyword);
                if (result.IsSuccess && result.Data != null)
                {
                    Consultations.Clear();
                    foreach (var consultation in result.Data.Items)
                    {
                        Consultations.Add(consultation);
                    }
                }
                else
                {
                    ShowErrorMessage($"��������ʧ��: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�������Ƽ�¼ʧ��");
                // ��������ʧ��
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            await LoadDataAsync();
        }

        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            await LoadDataAsync();
        }

        private void ViewDetails()
        {
            if (SelectedConsultation != null)
            {
                // �򵥵�������ʾ�����漰���ӵ���
                Logger.LogInformation("�鿴���Ƽ�¼����: {ConsultationId}", SelectedConsultation.Id);
            }
        }

        #endregion ���ݲ���
    }
}
