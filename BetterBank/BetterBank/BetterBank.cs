using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using GoldDigger;
using System.Collections.Generic;

namespace BetterBank
{
    public class BetterBank
    {
        static Plugin plugin;
        private static Harmony patcher = null;

        private static ConfigEntry<float> interestRate { get; set; }
        private static ConfigEntry<bool> enabled { get; set; }




        static AccessTools.FieldRef<BankLoanGUI, int> MaxLoanRef = AccessTools.FieldRefAccess<BankLoanGUI, int>("_MaxLoan");
        static AccessTools.FieldRef<Finance, List<StatsPanel.StatsItem>> DailyItemsRef = AccessTools.FieldRefAccess<Finance, List<StatsPanel.StatsItem>>("DailyItems");





        public BetterBank(Plugin pPlugin)
        {
            plugin = pPlugin;

            interestRate = plugin.Config.Bind<float>("Bank.Loan", "Interest", 5, new ConfigDescription("Interest compounded daily", new AcceptableValueRange<float>(0, 100)));
            enabled = plugin.Config.Bind<bool>("Bank.Loan", "Enabled", true, new ConfigDescription("Enable this mod", new AcceptableValueList<bool>(false, true)));

            if (enabled.Value && patcher == null)
            {
                patcher = Harmony.CreateAndPatchAll(typeof(BetterBank));
            }

            //Plugin.Log.LogMessage("DAYILY_STATS_CREDIT_PART: " + new LocalizationKey("DAYILY_STATS_CREDIT_PART").GetLocalized());

            enabled.SettingChanged += Enabled_SettingChanged;
        }

        private void Enabled_SettingChanged(object sender, System.EventArgs e)
        {
            if (enabled.Value && patcher == null)
            {
                patcher = Harmony.CreateAndPatchAll(typeof(BetterBank));
            }
            else
            {
                patcher.UnpatchAll();
                patcher = null;
            }
        }

        /*
        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.RefreshText))]
        [HarmonyPrefix]
        static bool Pre_RefreshText(ref string __result)
        {
            __result = "test";
            return true; // false to skip original
        }
        */


        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.RefreshText))]
        [HarmonyPostfix]
        static void Post_RefreshText(BankLoanGUI __instance)
        {
            BankLoanGUI self = __instance;

            float days = Mathf.Round(self.LoanDay) * 3f;
            float totalLoan = Mathf.RoundToInt(self.LoanAmount);
            float interest = 0;

            if (Finance.Instance.Credits.Count == 0)
            {
                interest = (CalcCreditSum(1, calculatePercent(), 1, days) - 1) * 100.0f;

            }
            else
            {
                days = Finance.Instance.Credits.First().DayToPay;
                totalLoan = Finance.Instance.Credits.First().MoneyToPay;
                interest = (CalcCreditSum(1, calculatePercent(), 1, days) - 1) * 100.0f;

                self.CurrentLoanText.text = string.Format("{0:0.00}", totalLoan) + "<size=\"60\">$</size>";
                self.LoanBalanceText.text = string.Format("{0:0.00}", totalLoan) + "<size=\"60\">$</size>";
                self.LoanAmountText.text = Mathf.RoundToInt(totalLoan).ToString();
                self.LoanDaysText.text = Mathf.RoundToInt(days).ToString();
                self.LoanAmountImage.fillAmount = totalLoan / (float)MaxLoanRef(__instance);
                self.InstallmentsImage.fillAmount = (days / 3) / self.MaxDays * 3f;


                //self.InterestRateText.text = interestRate.Value + "%";
                //self.CurrentLoanText.text = string.Format("{0:0.00}", totalLoan) + "<size=\"60\">$</size>";
                //self.LoanBalanceText.text = string.Format("{0:0.00}", totalLoan) + "<size=\"60\">$</size>";
                //self.LoanAmountText.text = string.Format("{0:0.00}", totalLoan) + "<size=\"60\">$</size>";
            }
            self.InterestRateText.text = string.Format("{0:0.00}", interest) + "%";
            self.DailyChargeText.text = string.Format("{0:0.00}", 0) + "<size=\"60\">$</size>";
        }



        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.calculatePercent))]
        [HarmonyPostfix]
        static float Post_calculatePercent(float original)
        {
            return calculatePercent();
        }

        static float calculatePercent()
        {
            return interestRate.Value / 100.0f;
        }

        [HarmonyPatch(typeof(Finance), nameof(Finance.GetCreditPart))]
        [HarmonyPrefix]
        static bool Pre_GetCreditPart(Finance __instance)
        {
            Finance self = __instance;

            for (int i = 0; i < self.Credits.Count; i++)
            {
                Finance.Credit credit = self.Credits[i];

                // add daily interest to amount
                credit.MoneyToPay = Mathf.RoundToInt((float)credit.MoneyToPay * (1f + calculatePercent()));

                credit.DayToPay--;

                // TODO: Decide what to do when they run out of days to pay
                // for now, we'll just debit the remaining amount of the loan

                if (credit.DayToPay < 0)
                {
                    GoldDigger.GameStateManager.Instance.Cash -= credit.MoneyToPay;
                    self.Credits.RemoveAt(i);
                    i--;
                }
            }

            return false; // prevent original
        }

        static float takingLoanOf = 0;

        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.TakeLoan))]
        [HarmonyPrefix]
        static void Pre_TakeLoan(BankLoanGUI __instance)
        {
            takingLoanOf = (float)(Mathf.RoundToInt(__instance.LoanAmount) * __instance.LoanClamp);
        }

        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.TakeLoan))]
        [HarmonyPostfix]
        static void Post_TakeLoan(BankLoanGUI __instance)
        {
            Finance.Instance.Credits.First().MoneyToPay = (int)takingLoanOf;

            __instance.RefreshText();
        }


        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.CalcCreditSum))]
        [HarmonyPrefix]
        static bool Pre_CalcCreditSum(ref float __result, float N, float r, float k, float n)
        {

            __result = CalcCreditSum(N, r, k, n);

            return false; // prevent original
        }

        static float CalcCreditSum(float N, float r, float k, float n)
        {
            float total = N;
            for (int i = 0; i < n; i++)
            {
                total *= (1 + r);
            }

            return total;
        }

        [HarmonyPatch(typeof(BankLoanGUI), nameof(BankLoanGUI.CalcCredirPerDay))]
        [HarmonyPrefix]
        static bool Pre_CalcCreditPerDay(BankLoanGUI __instance, ref float __result, float N, float r, float k, float n)
        {
            __result = CalcCreditSum(N, r, k, n) / n;

            return false; // prevent original
        }


        [HarmonyPatch(typeof(Finance), nameof(Finance.CollectDailyData))]
        [HarmonyPostfix]
        static void Post_CollectDailyData(Finance __instance)
        {
            if (Finance.Instance.Credits.Count > 0 && Finance.Instance.Credits.First().MoneyToPay > 0)
            {
                string toFind = new LocalizationKey("DAYILY_STATS_CREDIT_PART").GetLocalized();

                StatsPanel.StatsItem toRemove = null;

                foreach (var item in DailyItemsRef(__instance))
                {
                    if (item.Theme == toFind)
                    {
                        toRemove = item;
                    }
                }

                if (toRemove != null)
                {
                    DailyItemsRef(__instance).Remove(toRemove);
                }


                float interest = Finance.Instance.Credits.First().MoneyToPay / 1.05f * 0.05f;

                DailyItemsRef(__instance).Add(new StatsPanel.StatsItem("Interest Added To Loan", interest.ToString(), "$"));
            }
        }


    }





}
