/// FissionGenerator
/// ---------------------------------------------------
/// A generator module that consumes availableHeat from a FissionReactor
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace NearFutureElectrical
{
    public class FissionGenerator: FissionConsumer
    {

        // Maximum power generation
        [KSPField(isPersistant = false)]
        public float PowerGeneration = 100f;

        // Current power generation
        [KSPField(isPersistant = true)]
        public float CurrentGeneration = 0f;

        // Reactor Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Generation")]
        public string GeneratorStatus;

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_NFElectrical_ModuleFissionGenerator_PartInfo", PowerGeneration.ToString("F0"));
        }
        public string GetModuleTitle()
        {
            return "FissionGenerator";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_NFElectrical_ModuleFissionGenerator_ModuleName");
        }
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            Fields["GeneratorStatus"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionGenerator_Field_GeneratorStatus");
        }

        public void FixedUpdate()
        {
          if (HighLogic.LoadedScene == GameScenes.FLIGHT)
          {

              if (Status)
              {

                  double generated = (double)(Mathf.Clamp01(CurrentHeatUsed / HeatUsed) * PowerGeneration);
                    
                  vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out double currentEC, out double maxEC);
                  double delta = maxEC - currentEC;

                  double generatedActual = Math.Min(delta, TimeWarp.fixedDeltaTime * generated);

                  double amt = part.RequestResource("ElectricCharge",  -generatedActual);

                  if (double.IsNaN(generated))
                      generated = 0.0;

                  CurrentGeneration = (float)generatedActual / TimeWarp.fixedDeltaTime;
                  GeneratorStatus = String.Format("{0:F1} Ec/s", CurrentGeneration);
              }
              else
                  GeneratorStatus = Localizer.Format("#LOC_NFElectrical_ModuleFissionGenerator_Field_GeneratorStatus_Offline");
          }

        }


    }
}
