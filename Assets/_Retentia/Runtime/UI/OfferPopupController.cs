// -----------------------------------------------------------------------------
// OfferPopupController.cs
// Animated offer popup with DoTween transitions.
// Part of the Retentia AI - Player Retention System
// -----------------------------------------------------------------------------

using System;
using Cysharp.Threading.Tasks;
using Retentia.Data;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Retentia.Core
{
    #region Offer Popup Controller

    /// <summary>
    /// Controls an animated offer popup with DoTween sequences.
    /// </summary>
    public class OfferPopupController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Container")]
        [SerializeField] private RectTransform _popupContainer;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundOverlay;

        [Header("Content")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;

        [Header("Pricing")]
        [SerializeField] private Text _priceText;
        [SerializeField] private Text _originalPriceText;
        [SerializeField] private Text _discountText;
        [SerializeField] private GameObject _discountBadge;

        [Header("Timer")]
        [SerializeField] private GameObject _timerContainer;
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _timerFill;

        [Header("Buttons")]
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Text _acceptButtonText;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private float _showDuration = 0.5f;
        [SerializeField] private float _hideDuration = 0.3f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        #endregion

        #region Private Fields

        private Sequence _currentSequence;
        private OfferDefinitionSO _currentOffer;
        private Action _onAccept;
        private Action _onDecline;
        private DateTime _expiresAt;
        private bool _isShowing;

        #endregion

        #region Properties

        public bool IsShowing => _isShowing;
        public OfferDefinitionSO CurrentOffer => _currentOffer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initial hidden state
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // Setup button listeners
            _acceptButton?.onClick.AddListener(OnAcceptClicked);
            _declineButton?.onClick.AddListener(OnDeclineClicked);
            _closeButton?.onClick.AddListener(OnDeclineClicked);
        }

        private void Update()
        {
            if (_isShowing && _timerContainer != null && _timerContainer.activeSelf)
            {
                UpdateTimer();
            }
        }

        private void OnDestroy()
        {
            _currentSequence?.Kill();
            DOTween.Kill(transform);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the popup with an offer.
        /// </summary>
        public void ShowOffer(OfferDefinitionSO offer, Action onAccept = null, Action onDecline = null, DateTime? expiresAt = null)
        {
            if (_isShowing)
            {
                Hide();
            }

            _currentOffer = offer;
            _onAccept = onAccept;
            _onDecline = onDecline;
            _expiresAt = expiresAt ?? DateTime.UtcNow.AddSeconds(offer.ValidityDuration);

            // Populate content
            PopulateContent(offer);

            // Animate show
            AnimateShow();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public void Hide()
        {
            if (!_isShowing) return;

            AnimateHide();
        }

        #endregion

        #region Private Methods

        private void PopulateContent(OfferDefinitionSO offer)
        {
            if (_titleText != null)
            {
                _titleText.text = offer.DisplayName;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = offer.Description;
            }

            if (_iconImage != null && offer.Icon != null)
            {
                _iconImage.sprite = offer.Icon;
                _iconImage.gameObject.SetActive(true);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = offer.BackgroundColor;
            }

            // Pricing
            if (offer.IsFree)
            {
                if (_priceText != null)
                {
                    _priceText.text = "FREE";
                }
                if (_originalPriceText != null)
                {
                    _originalPriceText.gameObject.SetActive(false);
                }
                if (_discountBadge != null)
                {
                    _discountBadge.SetActive(false);
                }
            }
            else
            {
                if (_priceText != null)
                {
                    if (offer.PriceRealMoney > 0)
                    {
                        _priceText.text = $"${offer.PriceRealMoney:F2}";
                    }
                    else if (offer.PriceHardCurrency > 0)
                    {
                        _priceText.text = $"{offer.PriceHardCurrency} 💎";
                    }
                    else
                    {
                        _priceText.text = $"{offer.PriceSoftCurrency} 🪙";
                    }
                }

                if (offer.DiscountPercent > 0)
                {
                    if (_originalPriceText != null)
                    {
                        _originalPriceText.gameObject.SetActive(true);
                        _originalPriceText.text = $"${offer.OriginalPrice:F2}";
                    }

                    if (_discountBadge != null && _discountText != null)
                    {
                        _discountBadge.SetActive(true);
                        _discountText.text = $"-{offer.DiscountPercent}%";
                    }
                }
                else
                {
                    if (_originalPriceText != null)
                    {
                        _originalPriceText.gameObject.SetActive(false);
                    }
                    if (_discountBadge != null)
                    {
                        _discountBadge.SetActive(false);
                    }
                }
            }

            // Timer
            if (_timerContainer != null)
            {
                _timerContainer.SetActive(offer.ShowTimer);
            }

            // Accept button text
            if (_acceptButtonText != null)
            {
                _acceptButtonText.text = offer.IsFree ? "Claim Now!" : "Buy Now!";
            }
        }

        private void AnimateShow()
        {
            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            _isShowing = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            // Fade in overlay
            if (_backgroundOverlay != null)
            {
                _backgroundOverlay.DOFade(0.7f, _showDuration * 0.5f);
            }

            // Scale and fade popup
            if (_popupContainer != null)
            {
                _popupContainer.localScale = Vector3.one * 0.5f;
                _currentSequence.Append(_popupContainer.DOScale(1f, _showDuration).SetEase(_showEase));
            }

            if (_canvasGroup != null)
            {
                _currentSequence.Join(_canvasGroup.DOFade(1f, _showDuration * 0.7f));
            }

            // Special animation for discount badge
            if (_discountBadge != null && _discountBadge.activeSelf)
            {
                _discountBadge.transform.localScale = Vector3.zero;
                _currentSequence.Append(
                    _discountBadge.transform.DOScale(1f, 0.3f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(0.2f)
                );

                // Pulse animation
                _discountBadge.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(_showDuration + 0.3f);
            }

            DebugLogger.Debug($"[OfferPopup] Showing: {_currentOffer?.DisplayName}");
        }

        private void AnimateHide()
        {
            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            // Scale down and fade
            if (_popupContainer != null)
            {
                _currentSequence.Append(_popupContainer.DOScale(0.8f, _hideDuration).SetEase(_hideEase));
            }

            if (_canvasGroup != null)
            {
                _currentSequence.Join(_canvasGroup.DOFade(0f, _hideDuration));
            }

            if (_backgroundOverlay != null)
            {
                _currentSequence.Join(_backgroundOverlay.DOFade(0f, _hideDuration));
            }

            _currentSequence.OnComplete(() =>
            {
                _isShowing = false;
                if (_canvasGroup != null)
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
                _currentOffer = null;
            });

            DebugLogger.Debug("[OfferPopup] Hidden");
        }

        private void UpdateTimer()
        {
            var remaining = _expiresAt - DateTime.UtcNow;

            if (remaining.TotalSeconds <= 0)
            {
                OnDeclineClicked();
                return;
            }

            if (_timerText != null)
            {
                if (remaining.TotalHours >= 1)
                {
                    _timerText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                }
                else
                {
                    _timerText.text = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                }
            }

            if (_timerFill != null && _currentOffer != null)
            {
                float totalDuration = _currentOffer.ValidityDuration;
                float remainingPercent = (float)remaining.TotalSeconds / totalDuration;
                _timerFill.fillAmount = remainingPercent;

                // Change color as time runs out
                if (remainingPercent < 0.2f)
                {
                    _timerFill.color = Color.red;
                }
                else if (remainingPercent < 0.5f)
                {
                    _timerFill.color = Color.yellow;
                }
            }
        }

        private void OnAcceptClicked()
        {
            DebugLogger.Info($"[OfferPopup] Accepted: {_currentOffer?.DisplayName}");
            _onAccept?.Invoke();
            Hide();
        }

        private void OnDeclineClicked()
        {
            DebugLogger.Info($"[OfferPopup] Declined: {_currentOffer?.DisplayName}");
            _onDecline?.Invoke();
            Hide();
        }

        #endregion
    }

    #endregion
}
