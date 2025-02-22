﻿using System;

namespace CreditCardApplications
{
    public class CreditCardApplicationEvaluator
    {
        private readonly IFrequentFlyerNumberValidator _validator;
        private readonly FraudLookup _fraudLookup;

        private const int AutoReferralMaxAge = 20;
        private const int HighIncomeThreshold = 100_000; //Wartość wysokiego progu dochodów
        private const int LowIncomeThreshold = 20_000; //Wartość niskiego progu dochodów

        public int ValidatorLookupCount { get; private set; }

        public CreditCardApplicationEvaluator(IFrequentFlyerNumberValidator validator, FraudLookup fraudLookup = null)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _validator.ValidatorLookupPerformed += ValidatorLookupPerformed;
            _fraudLookup = fraudLookup;
        }

        private void ValidatorLookupPerformed(object sender, EventArgs e)
        {
            ValidatorLookupCount++;
        }

        public CreditCardApplicationDecision Evaluate(CreditCardApplication application)
        {
            if (_fraudLookup != null && _fraudLookup.IsFraudRisk(application)) return CreditCardApplicationDecision.ReferredToHumanFraudRisk;

            if (application.GrossAnnualIncome >= HighIncomeThreshold) return CreditCardApplicationDecision.AutoAccepted;

            if (_validator.ServiceInformation.License.LicenseKey == "EXPIRED") return CreditCardApplicationDecision.ReferredToHuman;

            _validator.ValidationMode = application.Age >= 30 ? ValidationMode.Detailed : ValidationMode.Quick;

            bool isValidFrequentFlyerNumber;

            try
            {
                isValidFrequentFlyerNumber = _validator.IsValid(application.FrequentFlyerNumber);
            }
            catch (Exception)
            {

                return CreditCardApplicationDecision.ReferredToHuman;
            }

            if (!isValidFrequentFlyerNumber) return CreditCardApplicationDecision.ReferredToHuman;

            if (application.Age <= AutoReferralMaxAge) return CreditCardApplicationDecision.ReferredToHuman;

            if (application.GrossAnnualIncome < LowIncomeThreshold) return CreditCardApplicationDecision.AutoDeclined;

            return CreditCardApplicationDecision.ReferredToHuman;
        }
    }
}
