using System;
using NUnit.Framework;
using UnityEngine;

namespace BallQuest.Tests
{
    public sealed class ArcadeProgressTests
    {
        [Test]
        public void SameTravelMadeFasterAddsMoreMomentum()
        {
            var travel=new Vector2(0,.3f);
            float fast=BallInput.WeightSwipe(travel,.06f).y;
            float slow=BallInput.WeightSwipe(travel,.6f).y;
            Assert.Greater(fast,slow*1.5f);
            Assert.AreEqual(0,BallInput.WeightSwipe(Vector2.zero,.016f).sqrMagnitude);
        }
        [Test]
        public void SwipeWeightIsIndependentOfSampleSubdivision()
        {
            var whole=BallInput.WeightSwipe(new Vector2(0,.3f),.3f);
            var sum=Vector2.zero;
            for(int i=0;i<10;i++)sum+=BallInput.WeightSwipe(new Vector2(0,.03f),.03f);
            Assert.Less((whole-sum).magnitude,.0001f);
        }
        [Test]
        public void DailyClaimCannotBeRepeatedAndSurvivesValidation()
        {
            var data=new SaveData();Assert.IsTrue(ArcadeProgress.ClaimDaily(data));
            Assert.AreEqual(50,data.coins);Assert.IsFalse(ArcadeProgress.ClaimDaily(data));
            data.Validate();Assert.IsFalse(ArcadeProgress.CanClaimDaily(data));
        }
        [Test]
        public void ConsecutiveDayClaimsAdvanceToGemReward()
        {
            var data=new SaveData{dailyClaimDate=DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),dailyIndex=1};
            Assert.AreEqual(1,ArcadeProgress.DailyDay(data));Assert.IsTrue(ArcadeProgress.ClaimDaily(data));Assert.AreEqual(1,data.gems);
        }
        [Test]
        public void MissionsPayOnlyAfterObjectiveAndOnlyOnce()
        {
            var data=new SaveData();Assert.IsFalse(ArcadeProgress.ClaimMission(data,"coin-scout"));
            data.lifetimeCoins=50;Assert.IsTrue(ArcadeProgress.ClaimMission(data,"coin-scout"));Assert.AreEqual(100,data.coins);
            Assert.IsFalse(ArcadeProgress.ClaimMission(data,"coin-scout"));Assert.AreEqual(100,data.coins);
        }
        [Test]
        public void FreeSpinAndGemExchangeCannotDuplicateOrMintInvalidAmounts()
        {
            var data=new SaveData();ArcadeProgress.FreeSpin(data);int coins=data.coins,gems=data.gems;
            ArcadeProgress.FreeSpin(data);Assert.AreEqual(coins,data.coins);Assert.AreEqual(gems,data.gems);Assert.AreEqual(1,data.lifetimeSpins);
            data.gems=3;Assert.IsFalse(ArcadeProgress.ExchangeGems(data,1,10000));
            Assert.IsTrue(ArcadeProgress.ExchangeGems(data,2,200));Assert.AreEqual(1,data.gems);
        }
    }
}
