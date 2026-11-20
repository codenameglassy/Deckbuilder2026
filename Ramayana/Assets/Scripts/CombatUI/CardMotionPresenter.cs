// Card motion for the combat HUD, drawn on its own motion canvas above the hand so a flight never rebuilds the hand
// or the HUD panels. The hand layout keeps owning where resting cards sit; this presenter flies pooled copies:
//   Draw: a copy flies from the draw pile into the card's live hand slot, which stays hidden until the copy lands.
//         Draws in one burst are staggered, and wait for a reshuffle to finish.
//   Play: the copy rises to the play zone, holds, then shrinks into the discard pile (or fades out if exhausted).
//   Discard from hand (end of turn): the copies sweep into the discard pile one after another.
//   Reshuffle: a few card backs fly from the discard pile to the draw pile.
// A card that leaves the hand mid-draw is retargeted from wherever its copy is. HandPresenter calls DrawIntoHand and
// LaunchFromHand; the deck's own events mark exhausts and reshuffles. Copies come from ObjectPool<CardFlight> and are
// reset on release. A face copy shows the same live description numbers as the hand card it stands in for.
// Drawn cards fly in along an upward arc, face down at first, flip face up partway and swell slightly as they land;
// the end-of-turn sweep arcs into the discard pile with a spin away from the hand.
// Face-down copies and flip covers are drawn as card backs (CardFrameMaterials.ApplyBack), matching the pile stacks.
// All timings, scales and the play zone come from CombatHudStyleSO. It only presents.
using System;
using System.Collections.Generic;
using DG.Tweening;
using JourneyToLanka.Combat.Model;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JourneyToLanka.CombatUI
{
    internal sealed class CardMotionPresenter : IDisposable
    {
        private const int FaceCapacity = 12;
        private const int FaceMax = 40;
        private const int BackCapacity = 8;
        private const int BackMax = 24;
        private const float ExhaustScaleFactor = 0.8f;

        private readonly RectTransform _root;
        private readonly CardView _template;
        private readonly PileStackView _drawPile;
        private readonly PileStackView _discardPile;
        private readonly ObjectPool<CardFlight> _faces;
        private readonly ObjectPool<CardFlight> _backs;
        private readonly List<CardFlight> _created = new List<CardFlight>(FaceCapacity + BackCapacity);
        private readonly List<CardFlight> _active = new List<CardFlight>(FaceCapacity + BackCapacity);
        private readonly Dictionary<int, CardFlight> _byCard = new Dictionary<int, CardFlight>(FaceCapacity);
        private readonly Action<CardFlight> _drawArrived;
        private readonly Action<CardFlight> _playRaised;
        private readonly Action<CardFlight> _pileArrived;
        private readonly Action<CardFlight> _faded;
        private readonly Action<CardFlight> _backArrived;
        private CombatHudStyleSO _style;
        private CombatDeck _deck;
        private float _nextDrawTime;
        private float _nextSweepTime;

        public CardMotionPresenter(CardView template, RectTransform root, PileStackView drawPile,
            PileStackView discardPile)
        {
            _template = template;
            _root = root;
            _drawPile = drawPile;
            _discardPile = discardPile;
            _faces = new ObjectPool<CardFlight>(CreateFace, HandleGet, HandleRelease, HandleDestroy, true, FaceCapacity, FaceMax);
            _backs = new ObjectPool<CardFlight>(CreateBack, HandleGet, HandleRelease, HandleDestroy, true, BackCapacity, BackMax);
            _drawArrived = HandleDrawArrived;
            _playRaised = HandlePlayRaised;
            _pileArrived = HandlePileArrived;
            _faded = Release;
            _backArrived = HandleBackArrived;
        }

        public void SetStyle(CombatHudStyleSO style)
        {
            _style = style;
            if (_style == null)
            {
                return;
            }

            for (int i = 0; i < _created.Count; i++)
            {
                StyleFlight(_created[i]);
            }
        }

        public void Attach(CombatDeck deck)
        {
            Detach();
            _deck = deck;
            _deck.CardMoved += HandleCardMoved;
            _deck.Reshuffled += HandleReshuffled;
            Prewarm(_faces, deck.MaxHandSize);
        }

        public void Detach()
        {
            if (_deck == null)
            {
                return;
            }

            _deck.CardMoved -= HandleCardMoved;
            _deck.Reshuffled -= HandleReshuffled;
            _deck = null;
            StopAll();
        }

        public void Dispose()
        {
            Detach();
            _faces.Dispose();
            _backs.Dispose();
            _created.Clear();
        }

        public void DrawIntoHand(CardView handView, CardInstance card, CardPile from)
        {
            if (_style == null || _deck == null)
            {
                return;
            }

            ReleaseFlightOf(card.Id);
            handView.SetRevealed(false);

            CardFlight flight = GetFace(card, handView);
            flight.HandView = handView;
            PileStackView source = from == _deck.DiscardPile ? _discardPile : _drawPile;
            flight.Place(CardFlight.WorldCenter(source.Rect), _style.DrawStartScale, 0f);
            flight.SetPath(_style.DrawArcHeight, 0f);
            flight.SetFlip(_style.DrawFlipPoint, _style.DrawFlipWidth);
            flight.SetSwell(_style.DrawLandSwell);
            flight.FlyTo((RectTransform)handView.transform, 1f, 1f, _style.DrawDuration, Schedule(ref _nextDrawTime, _style.DrawStagger),
                Ease.OutCubic, _drawArrived);
        }

        public void LaunchFromHand(CardView handView, CardInstance card, CardPile to)
        {
            if (_style == null || _deck == null)
            {
                return;
            }

            if (_byCard.TryGetValue(card.Id, out CardFlight flight))
            {
                flight.HandView = null;
            }
            else
            {
                var handRect = (RectTransform)handView.transform;
                flight = GetFace(card, handView);
                flight.Place(CardFlight.WorldCenter(handRect), handRect.localScale.x, 1f, handRect.eulerAngles.z);
            }

            flight.Rect.SetAsLastSibling();

            if (to == _deck.InPlay)
            {
                flight.FlyTo(PlayZone(), _style.PlayScale, 1f, _style.PlayRiseDuration, 0f, Ease.OutCubic, _playRaised);
            }
            else if (to == _deck.DiscardPile)
            {
                float direction = CardFlight.WorldCenter(_discardPile.Rect).x >= flight.Rect.position.x ? -1f : 1f;
                flight.SetPath(_style.SweepArcHeight, _style.SweepSpin * direction);
                flight.FlyTo(_discardPile.Rect, _style.PileArrivalScale, _style.PileArrivalAlpha, _style.SweepDuration,
                    Schedule(ref _nextSweepTime, _style.SweepStagger), Ease.InCubic, _pileArrived);
            }
            else
            {
                FadeOut(flight, 0f);
            }
        }

        private void HandleCardMoved(CardInstance card, CardPile from, CardPile to)
        {
            if (from == _deck.InPlay && to == _deck.ExhaustPile && _byCard.TryGetValue(card.Id, out CardFlight flight))
            {
                flight.ExhaustOnExit = true;
            }
        }

        private void HandleReshuffled()
        {
            if (_style == null)
            {
                return;
            }

            int count = Mathf.Min(_style.ReshuffleCardCount, _deck.DrawPile.Count);
            if (count <= 0)
            {
                return;
            }

            Vector3 start = CardFlight.WorldCenter(_discardPile.Rect);
            for (int i = 0; i < count; i++)
            {
                CardFlight back = Get(_backs);
                back.Place(start, _style.PileArrivalScale, 1f);
                back.FlyTo(_drawPile.Rect, _style.PileArrivalScale, 1f, _style.ReshuffleDuration, i * _style.ReshuffleStagger,
                    Ease.InOutCubic, _backArrived);
            }

            float finished = Time.unscaledTime + (count - 1) * _style.ReshuffleStagger + _style.ReshuffleDuration;
            _nextDrawTime = Mathf.Max(_nextDrawTime, finished);
        }

        private void HandleDrawArrived(CardFlight flight)
        {
            CardView handView = flight.HandView;
            if (handView != null && handView.CardId == flight.CardId)
            {
                handView.SetRevealed(true);
            }

            Release(flight);
        }

        private void HandlePlayRaised(CardFlight flight)
        {
            if (flight.ExhaustOnExit)
            {
                FadeOut(flight, _style.PlayHold);
                return;
            }

            flight.FlyTo(_discardPile.Rect, _style.PileArrivalScale, _style.PileArrivalAlpha, _style.ToPileDuration, _style.PlayHold,
                Ease.InCubic, _pileArrived);
        }

        private void HandlePileArrived(CardFlight flight)
        {
            _discardPile.Punch(_style.PilePunchScale, _style.PilePunchDuration);
            Release(flight);
        }

        private void HandleBackArrived(CardFlight flight)
        {
            _drawPile.Punch(_style.PilePunchScale, _style.PilePunchDuration);
            Release(flight);
        }

        private void FadeOut(CardFlight flight, float delay)
        {
            flight.FlyTo(flight.Rect.position, flight.Rect.localScale.x * ExhaustScaleFactor, 0f, _style.ExhaustDuration, delay,
                Ease.InQuad, _faded);
        }

        private Vector3 PlayZone()
        {
            return _root.TransformPoint(_root.rect.center + _style.PlayZoneOffset);
        }

        private static float Schedule(ref float nextTime, float stagger)
        {
            float now = Time.unscaledTime;
            float delay = Mathf.Max(0f, nextTime - now);
            nextTime = now + delay + stagger;
            return delay;
        }

        private CardFlight GetFace(CardInstance card, CardView handView)
        {
            CardFlight flight = Get(_faces);
            flight.CardId = card.Id;
            flight.Face.Bind(card);
            flight.Face.SetTextContext(handView.TextContext);
            _byCard[card.Id] = flight;
            return flight;
        }

        private CardFlight Get(ObjectPool<CardFlight> pool)
        {
            CardFlight flight = pool.Get();
            _active.Add(flight);
            return flight;
        }

        private void ReleaseFlightOf(int cardId)
        {
            if (_byCard.TryGetValue(cardId, out CardFlight flight))
            {
                Release(flight);
            }
        }

        private void Release(CardFlight flight)
        {
            if (flight.CardId >= 0 && _byCard.TryGetValue(flight.CardId, out CardFlight mapped) && mapped == flight)
            {
                _byCard.Remove(flight.CardId);
            }

            _active.Remove(flight);
            if (flight.IsFace)
            {
                _faces.Release(flight);
            }
            else
            {
                _backs.Release(flight);
            }
        }

        private void StopAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CardFlight flight = _active[i];
                if (flight.HandView != null && flight.HandView.CardId == flight.CardId)
                {
                    flight.HandView.SetRevealed(true);
                }

                Release(flight);
            }

            _byCard.Clear();
            _nextDrawTime = 0f;
            _nextSweepTime = 0f;
        }

        private void StyleFlight(CardFlight flight)
        {
            if (flight.IsFace)
            {
                flight.Face.ApplyStyle(_style);
                StyleCover(flight);
                return;
            }

            flight.Rect.sizeDelta = _style.CardSize;
            CardFrameMaterials.ApplyBack(flight.Back, _style, Color.white);
        }

        private void StyleCover(CardFlight flight)
        {
            if (flight.Cover != null && _style != null)
            {
                CardFrameMaterials.ApplyBack(flight.Cover, _style, Color.white);
            }
        }

        private CardFlight CreateFace()
        {
            CardView face = Object.Instantiate(_template, _root);
            face.name = "FlyingCard";
            face.Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Image cover = UIBuildUtility.CreateImage("Cover", face.transform, false);
            UIBuildUtility.Stretch(cover.rectTransform);
            cover.enabled = false;
            face.gameObject.SetActive(false);
            return Register(new CardFlight((RectTransform)face.transform, face.Group, face, null, cover));
        }

        private CardFlight CreateBack()
        {
            Image back = UIBuildUtility.CreateImage("FlyingCardBack", _root, false);
            CanvasGroup group = back.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            back.gameObject.SetActive(false);
            return Register(new CardFlight(back.rectTransform, group, null, back, null));
        }

        private CardFlight Register(CardFlight flight)
        {
            _created.Add(flight);
            if (_style != null)
            {
                StyleFlight(flight);
            }

            return flight;
        }

        private static void HandleGet(CardFlight flight)
        {
            if (flight.IsFace)
            {
                flight.Face.OnTakenFromPool();
            }

            flight.Rect.gameObject.SetActive(true);
            flight.Rect.SetAsLastSibling();
        }

        private static void HandleRelease(CardFlight flight)
        {
            flight.Reset();
            if (flight.IsFace)
            {
                flight.Face.OnReturnedToPool();
            }

            flight.Rect.gameObject.SetActive(false);
        }

        private void HandleDestroy(CardFlight flight)
        {
            flight.Reset();
            _created.Remove(flight);
            if (flight.Rect != null)
            {
                Object.Destroy(flight.Rect.gameObject);
            }
        }

        private static void Prewarm(ObjectPool<CardFlight> pool, int count)
        {
            int missing = count - pool.CountInactive;
            if (missing <= 0)
            {
                return;
            }

            List<CardFlight> created = ListPool<CardFlight>.Get();
            for (int i = 0; i < missing; i++)
            {
                created.Add(pool.Get());
            }

            for (int i = 0; i < created.Count; i++)
            {
                pool.Release(created[i]);
            }

            ListPool<CardFlight>.Release(created);
        }
    }
}
