﻿/// FissionReactor
/// ---------------------------------------------------
/// Fission Generator!
///

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI;
using NearFutureElectrical.UI;
using KSP.Localization;

namespace NearFutureElectrical
{
    public class FissionReactor: ModuleResourceConverter
    {
        public struct ResourceBaseRatio
        {
            public string ResourceName;
            public double ResourceRatio;

            public ResourceBaseRatio(string name, double ratio)
            {
                ResourceName = name;
                ResourceRatio = ratio;
            }
         }

        /// CONFIGURABLE FIELDS
        // ----------------------
        // The icon to use in the reactor UI
        [KSPField(isPersistant = true)]
        public int UIIcon = 1;

        // The
        [KSPField(isPersistant = true)]
        public string UIName = "";

        // Whether reactor power settings should follow the throttle setting
        [KSPField(isPersistant = false)]
        public bool FollowThrottle = false;

        // Engage safety override
        [KSPField(isPersistant = true, guiActive = true, guiName = "Auto-Shutdown Temp"), UI_FloatRange(minValue = 700f, maxValue = 6000f, stepIncrement = 100f)]
        public float CurrentSafetyOverride = 1000f;

        // Heat generation at full power
        [KSPField(isPersistant = false)]
        public float HeatGeneration;

        // Nominal reactor temperature (where the reactor should live)
        [KSPField(isPersistant = false)]
        public float NominalTemperature = 900f;

        // Critical reactor temperature (core damage after this)
        [KSPField(isPersistant = false)]
        public float CriticalTemperature = 1400f;

        // Critical reactor temperature (core damage after this)
        [KSPField(isPersistant = false)]
        public float MaximumTemperature = 2000f;

        // Current reactor power setting (0-100, tweakable), default value set at OverriddenStart()
        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Setting", guiUnits ="%"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
        public float CurrentPowerPercent = -1f;

        // Actual reactor power setting used (0-100, read-only)
        [KSPField(isPersistant = false)]
        public float ActualPowerPercent = 100f;

        //enable automated mode
        [KSPField(isPersistant = true)]
        public bool AutomatedMode = true;

        // Curve relating available power to temperature. Generally should be of the form
        // AmbientTemp  0
        // NominalTemp RatedReactorOutput
        // MaxTemp BonusReactorOutput
        [KSPField(isPersistant = false)]
        public FloatCurve PowerCurve = new FloatCurve();

        // amount of heating power available from reactor currently
        [KSPField(isPersistant = true)]
        public float AvailablePower = 0f;

        // Name of the fuel
        [KSPField(isPersistant = false)]
        public string FuelName = "EnrichedUranium";

        // name of the overheat animation
        [KSPField(isPersistant = false)]
        public string OverheatAnimation;

        [KSPField(isPersistant = false)]
        public int smoothingInterval = 25;

        [KSPField(isPersistant = true)]
        public bool TimewarpShutdown = false;

        [KSPField(isPersistant = true)]
        public int TimewarpShutdownFactor  = 5;

        // REPAIR VARIABLES
        // integrity of the core
        [KSPField(isPersistant = true)]
        public float CoreIntegrity = 100f;

        // Rate the core is damaged, in % per S per K
        [KSPField(isPersistant = false)]
        public float CoreDamageRate = 0.005f;

        // Engineer level to repair the core
        [KSPField(isPersistant = false)]
        public int EngineerLevelForRepair = 5;

        [KSPField(isPersistant = false)]
        public float MaxRepairPercent = 75;

        [KSPField(isPersistant = false)]
        public float MinRepairPercent = 10;

        [KSPField(isPersistant = false)]
        public float MaxTempForRepair = 325;

        [KSPField(isPersistant = true)]
        public bool FirstLoad = true;

        /// UI ACTIONS
        /// --------------------
        /// Toggle control panel
        [KSPEvent(guiActive = true, guiName = "Toggle Reactor Control", active = true)]
        public void ShowReactorControl()
        {
            ReactorUI.ToggleReactorWindow();
        }
        [KSPAction("Toggle Reactor Panel")]
        public void TogglePanelAction(KSPActionParam param)
        {
            ShowReactorControl();
        }

        // Try to fix the reactor
        [KSPEvent(externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f, guiName = "Repair Reactor")]
        public void RepairReactor()
        {
            if (TryRepairReactor())
            {
              DoReactorRepair();
            }
        }
        /// DEBUG
        /// ----------------------

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_RealHeatGen")]
        public string D_RealHeat;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_RealConsumption")]
        public string D_RealConsumption;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_PowerScale")]
        public string D_PowerScale;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_TempScale")]
        public string D_TempScale;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_FudgeFactor")]
        public string D_FudgeFactor;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_FudgeCap")]
        public string D_FudgeCap;

        [KSPField(isPersistant = false, guiActive = false, guiName = "D_IsHeating")]
        public string D_IsHeating;

        /// PRIVATE VARIABLES
        /// ----------------------
        private ModuleCoreHeat core;

        private AnimationState[] overheatStates;

        // base paramters
        private List<ResourceBaseRatio> inputs;
        private List<ResourceBaseRatio> outputs;

        private FloatCurve throttleCurve;

        private FissionEngine reactorEngine;

        /// UI FIELDS
        /// --------------------


        // Reactor Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Reactor Power")]
        public string ReactorOutput;

        // Reactor Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Available Power")]
        public string ThermalTransfer;

        // integrity of the core
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temperature")]
        public string CoreTemp;

        // integrity of the core
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Health")]
        public string CoreStatus;

        // Fuel Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Life")]
        public string FuelStatus;

        // Sets whether auto-shutdown is possible
        public ModuleCoreHeat Core{ get {return core;}}

        // Sets whether time wwarp shutdown is enabled
        public void SetTimewarpShutdownStatus(bool status)
        {

        }

        public override string GetInfo()
        {
            double baseRate = 0d;
            for (int i = 0 ;i < inputList.Count;i++)
            {
                if (inputList[i].ResourceName == FuelName)
                    baseRate = inputList[i].Ratio;
            }
            return
                Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_PartInfo",
                (HeatGeneration / 50f).ToString("F0"),
                NominalTemperature.ToString("F0"),
                CriticalTemperature.ToString("F0"),
                MaximumTemperature.ToString("F0"),
                FindTimeRemaining(this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, baseRate))
                + base.GetInfo();
        }
        public string GetModuleTitle()
        {
            return "FissionReactor";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_ModuleName");
        }

        private void SetupResourceRatios()
        {

            inputs = new List<ResourceBaseRatio>();
            outputs = new List<ResourceBaseRatio>();

            for (int i = 0 ;i < inputList.Count;i++)
            {
                inputs.Add(new ResourceBaseRatio(inputList[i].ResourceName, inputList[i].Ratio));
            }
            for (int i = 0 ;i < outputList.Count;i++)
            {
                outputs.Add(new ResourceBaseRatio(outputList[i].ResourceName, outputList[i].Ratio));
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
        }


        public override void OnUpdate()
        {
            base.OnUpdate();

        }
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

        }

        public void OverriddenStart()
        {
          var range = (UI_FloatRange)this.Fields["CurrentSafetyOverride"].uiControlEditor;
          range.minValue = 0f;
          range.maxValue = MaximumTemperature;

          range = (UI_FloatRange)this.Fields["CurrentSafetyOverride"].uiControlFlight;
          range.minValue = 0f;
          range.maxValue = MaximumTemperature;

          throttleCurve = new FloatCurve();
          throttleCurve.Add(0, 0, 0, 0);
          throttleCurve.Add(50, 20, 0, 0);
          throttleCurve.Add(100, 100, 0, 0);

          Actions["TogglePanelAction"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Action_TogglePanelAction");

          Events["ShowReactorControl"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Event_ShowReactorControl");
          Events["RepairReactor"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Event_RepairReactor");

          Fields["CurrentSafetyOverride"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CurrentSafetyOverride");          
          Fields["ReactorOutput"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_ReactorOutput");
          Fields["ThermalTransfer"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_ThermalTransfer");
          Fields["CoreTemp"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CoreTemp");
          Fields["CoreStatus"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CoreStatus");
          Fields["FuelStatus"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus");

            if (AutomatedMode)
            {
                Fields["CurrentPowerPercent"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CurrentTargetEClevel");
                CurrentPowerPercent = CurrentPowerPercent < 0 ? 90 : CurrentPowerPercent;
            }
            else
            {
                Fields["CurrentPowerPercent"].guiName = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CurrentPowerPercent");
                CurrentPowerPercent = CurrentPowerPercent < 0 ? 100 : CurrentPowerPercent;
            }


            if (FirstLoad)
          {
            this.CurrentSafetyOverride = this.CriticalTemperature;
            FirstLoad = false;
          }

          if (HighLogic.LoadedScene != GameScenes.EDITOR)
          {
              core = this.GetComponent<ModuleCoreHeat>();
              if (core == null)
                  Utils.LogError("Fission Reactor: Could not find core heat module!");

              SetupResourceRatios();

              if (OverheatAnimation != "")
                  overheatStates = Utils.SetUpAnimation(OverheatAnimation, this.part);

              if (FollowThrottle)
                  reactorEngine = this.GetComponent<FissionEngine>();

          } else
          {
              //this.CurrentSafetyOverride = this.NominalTemperature;
          }
        }
        public void OverriddenUpdate()
        {
          if (HighLogic.LoadedScene == GameScenes.FLIGHT)
          {
              foreach (BaseField fld in base.Fields)
              {
                  if (fld.name == "status")
                      fld.guiActive = false;

              }
              if (core != null)
              {
                  core.CoreShutdownTemp = (double)CurrentSafetyOverride+10d;

              }
          }
        }

        private double lastCurrentEC = 0;
        private float percentPerPower = 1;

        public void OverriddenFixedUpdate()
        {
          if (HighLogic.LoadedScene == GameScenes.FLIGHT)
          {
              if (UIName == "")
                UIName = part.partInfo.title;
              if (FollowThrottle)
              {
                  if (reactorEngine != null)
                    ActualPowerPercent = Math.Max(throttleCurve.Evaluate(100 * this.vessel.ctrlState.mainThrottle * reactorEngine.GetThrustLimiterFraction()), CurrentPowerPercent);
              }
              else if(AutomatedMode)
                {
                    vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out double currentEC, out double maxEC);
                    double delta = (CurrentPowerPercent/100)*maxEC - currentEC;

                    double ECperSecond = (currentEC - lastCurrentEC) / TimeWarp.fixedDeltaTime;
                    lastCurrentEC = currentEC;

                    float cgp = CurrentGeneratedPower();

                    if (cgp > 0 && ActualPowerPercent > 0)
                        percentPerPower = ActualPowerPercent / cgp;

                    float currentLosses = cgp - (float)ECperSecond;

                    float targetECperSecond = (float)delta / 1f; //reach target in 1s

                    float newAPP = (currentLosses + targetECperSecond) * percentPerPower;                    
                    

                    //Debug.Log("BLACKLINE's awesome Calculations:");
                    //Debug.Log("BLACKLINE's ActualPowerPercent = " + ActualPowerPercent.ToString());
                    //Debug.Log("BLACKLINE's delta = " + delta.ToString());
                    //Debug.Log("BLACKLINE's ECperSecond = " + ECperSecond.ToString());
                    //Debug.Log("BLACKLINE's CurrentGeneratedPower = " + cgp.ToString());
                    //Debug.Log("BLACKLINE's powerPerPercent = " + percentPerPower.ToString());
                    //Debug.Log("BLACKLINE's currentLosses = " + currentLosses.ToString());
                    //Debug.Log("BLACKLINE's targetECperSecond = " + targetECperSecond.ToString());

                    //we got some time left 
                    ActualPowerPercent = Mathf.Clamp(newAPP, 0f, 100f);
                    //Debug.Log("BLACKLINE's new ActualPowerPercent = " + ActualPowerPercent.ToString());

                }
                else
                {
                    ActualPowerPercent = CurrentPowerPercent;
                }

                // Update reactor core integrity readout
                if (CoreIntegrity > 0)
                  CoreStatus = String.Format("{0:F2} %", CoreIntegrity);
              else
                  CoreStatus = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_CoreStatus_Meltdown");


              // Handle core damage tracking and effects
              HandleCoreDamage();
              // Heat consumption occurs if reactor is on or off
              DoHeatConsumption();

              // IF REACTOR ON
              // =============
              if (base.ModuleIsActive())
              {
                if (TimewarpShutdown && TimeWarp.fetch.current_rate_index >= TimewarpShutdownFactor)
                    ToggleResourceConverterAction(new KSPActionParam(0, KSPActionType.Activate));

                DoFuelConsumption();
                DoHeatGeneration();

              }
              // IF REACTOR OFF
              // =============
              else
              {
                  // Update UI
                  if (CoreIntegrity <= 0f)
                  {
                      FuelStatus = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus_Meltdown");
                      ReactorOutput = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_ReactorOutput_Meltdown");
                  }
                  else
                  {
                      FuelStatus = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus_Offline");
                      ReactorOutput = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_ReactorOutput_Offline");

                  }
              }
              lastTimeWarpMult = TimeWarp.CurrentRate;
          }
        }



        private void DoFuelConsumption()
        {
            if (_recipe != null && _recipe.Inputs != null)
            {

                // Get current resource consumption
                double rate = 0d;
                for (int i = 0; i < inputList.Count; i++)
                {
                    if (_recipe.Inputs[i].ResourceName == FuelName)
                        rate = _recipe.Inputs[i].Ratio;
                }
                // Recalculate fuel use Ratio
                // Fuel use is proportional to power setting
                RecalculateRatios(ActualPowerPercent / 100f);

                // Find the time remaining at current rate
                FuelStatus = FindTimeRemaining( this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(FuelName).id).amount, rate);
            }
        }

        // Creates heat from the reaction
        private void DoHeatGeneration()
        {
            // Generate heat from the reaction and apply it
            SetHeatGeneration((ActualPowerPercent / 100f * HeatGeneration)* CoreIntegrity/100f);

            if (CoreIntegrity <= 0f)
            {
                FuelStatus = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus_Meltdown");
                ReactorOutput = Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_ReactorOutpur_Meltdown");
            }
            else
            {
                ReactorOutput = String.Format("{0:F1} kW", ActualPowerPercent / 100f * HeatGeneration / 50f * CoreIntegrity / 100f);
            }
        }

        List<float> availablePowerList = new List<float>();
        float reactorFudgeFactor = 0f;

        List<float> framePowerList = new List<float>();
        float lastTimeWarpMult = 1f;

        private void DoHeatConsumption()
        {
            // save some divisions later
            float coreIntegrity = CoreIntegrity / 100f;
            float reactorThrottle = ActualPowerPercent / 100f;
            if (!base.ModuleIsActive())
                reactorThrottle = 0f;
            float maxHeatGenerationKW = HeatGeneration / 50f;

            // The core temperature where no generation is possible
            float zeroPoint = (float)part.temperature;

            // The core temperature where maximum generation is possible
            float maxPoint = NominalTemperature;

            float temperatureDiff = Mathf.Clamp((float)core.CoreTemperature - zeroPoint, 0f, NominalTemperature);

            // The fraction of generation that is possible.
            float curTempScale = Mathf.Clamp(temperatureDiff / (maxPoint - zeroPoint),0f,1f);

            // Fraction showing amount of power available to
            float powerScale = Mathf.Min(reactorThrottle, curTempScale)*coreIntegrity;

            AvailablePower = powerScale * maxHeatGenerationKW;

            // Allocate power to generators/engines
            if (float.IsNaN(AvailablePower))
                AvailablePower = 0f;

            AllocateThermalPower();

            // GUI
            ThermalTransfer = String.Format("{0:F1} kW", AvailablePower);
            CoreTemp = String.Format("{0:F1}/{1:F1} K", (float)core.CoreTemperature, NominalTemperature);

            D_TempScale = String.Format("{0:F4}", curTempScale);
            D_PowerScale = String.Format("{0:F4}", powerScale);
        }

        private void AllocateThermalPower()
        {
            List<FissionConsumer> consumers = GetOrderedConsumers();
            //Utils.Log("FissionReactor: START CYCLE: has " + AvailablePower.ToString() +" kW to distribute");
            float remainingPower = AvailablePower;
            // Iterate through all consumers and allocate available thermal power
            for (int i= 0; i < consumers.Count; i++)
            {
                if (consumers[i].Status)
                {
                    remainingPower = consumers[i].ConsumeHeat(remainingPower);
                    //totalWaste = totalWaste + consumer.GetWaste();

                    if (remainingPower <= 0f)
                        remainingPower = 0f;
                    //Utils.Log ("FissionReactor: Consumer left "+ remainingPower.ToString()+ " kW");
                }
            }
        }

        private float CurrentGeneratedPower()
        {
            List<FissionConsumer> consumers = GetOrderedConsumers();

            float CurrentGeneration = 0;

            // Iterate through all consumers and sum up CurrentGeneration
            for (int i = 0; i < consumers.Count; i++)
            {
                if (consumers[i].Status)
                {
                    if (consumers[i] is FissionGenerator)
                    {
                        CurrentGeneration += ((FissionGenerator)consumers[i]).CurrentGeneration;
                    }
                }
            }

            return CurrentGeneration;
        }

        private void DoHeatConsumption_V1()
        {
            // determine the maximum radiator cooling
            // At ambient temperature part temperature, no cooling is possible
            // at nominal temperature, full cooling is possible

            // The core temperature where no cooling is possible
            float zeroPoint = (float)part.temperature;

            // The core temperature where maximum cooling is possible (above here allow more cooling if needed)
            float maxPoint = NominalTemperature;

            float temperatureDiff = Mathf.Clamp((float)core.CoreTemperature - zeroPoint, 0f, NominalTemperature);

            // The ratio (0 to 1+) of radiator capacity to use at the moment.
            float curTempScale = Mathf.Clamp(temperatureDiff / (maxPoint - zeroPoint),0f,1f);

            // The allowed maximum radiator cooling. Should not exceed the heat generation
            float coolingCap = HeatGeneration / 50f;

            float maxRadiatorCooling = Mathf.Clamp(curTempScale * (HeatGeneration / 50f) * (CoreIntegrity/100f) *(ActualPowerPercent/100f),
                0f,
                coolingCap);


            // Determine power available to transfer to components
            // This can be unstable so smooth it.
            float frameAvailablePower = 0f;
            if (Single.TryParse(core.D_CoolAmt, out frameAvailablePower))
            {
                framePowerList.Add(frameAvailablePower / Mathf.Clamp(TimeWarp.CurrentRate, 0f, (float)core.MaxCalculationWarp));
                if (framePowerList.Count > smoothingInterval)
                {
                    framePowerList.RemoveAt(0);
                }
            }

            float smoothedPower = ListMean(framePowerList);

            float maxFudge = (float)core.MaxCoolant;

            // The reactor fudge factor is a number by which we increase the reactor power to pretend radiators are
            // transferring less at low temperatures
            reactorFudgeFactor =  Mathf.Clamp(smoothedPower - maxRadiatorCooling,0f,maxFudge);

            // The available power is never more than the max radiator cooling
            AvailablePower = Mathf.Clamp(smoothedPower,0f, maxRadiatorCooling);

            if (float.IsNaN(AvailablePower))
                AvailablePower = 0f;

            D_RealConsumption = String.Format("{0:F4}", frameAvailablePower);
            D_TempScale = String.Format("{0:F4}", curTempScale);
            D_FudgeFactor = String.Format("{0:F4}", reactorFudgeFactor);
            D_FudgeCap = String.Format("{0:F4}", maxFudge);
            //D_MaxCooling = String.Format("{0:F4}", maxRadiatorCooling);
            D_IsHeating = GeneratesHeat.ToString();
            ThermalTransfer = String.Format("{0:F2} kW", AvailablePower);
            CoreTemp = String.Format("{0:F1}/{1:F1} K", (float)core.CoreTemperature, NominalTemperature);

            // Core temperature goal is always artificially lower
            core.CoreTempGoalAdjustment = -core.CoreTempGoal;

            List<FissionConsumer> consumers = GetOrderedConsumers();

            //Utils.Log("FissionReactor: START CYCLE: has " + AvailablePower.ToString() +" kW to distribute");
            float remainingPower = AvailablePower;
            // Iterate through all consumers and allocate available thermal power
            for (int i= 0; i < consumers.Count; i++)
            {
                if (consumers[i].Status)
                {
                    remainingPower = consumers[i].ConsumeHeat(remainingPower);
                    //totalWaste = totalWaste + consumer.GetWaste();

                    if (remainingPower <= 0f)
                        remainingPower = 0f;
                    //Utils.Log ("FissionReactor: Consumer left "+ remainingPower.ToString()+ " kW");
                }
            }

            //Utils.Log ("FissionReactor: END CYCLE with "+totalWaste.ToString() + " waste, and " + AvailablePower.ToString() +" spare");
        }

        // Get the mean of a list
        private float ListMean(List<float> theList)
        {
            float sum = 0f;
            for (int i = 0; i < theList.Count;i++)
            {
                sum += theList[i];
            }
            return sum / (float)theList.Count;
        }



        private List<FissionConsumer> GetOrderedConsumers()
        {
          List<FissionConsumer> consumers = this.GetComponents<FissionConsumer>().ToList();
          return consumers.OrderBy(o=>o.Priority).ToList();
        }

        private void SetHeatGeneration(float heat)
        {
            if (Time.timeSinceLevelLoad > 5f)
                GeneratesHeat = true;
            else
                GeneratesHeat = false;

            //Utils.Log("Fudge Factor currently " + reactorFudgeFactor.ToString());
            if (float.IsNaN(reactorFudgeFactor))
            {
                reactorFudgeFactor = 0f;
            }

            TemperatureModifier = new FloatCurve();
            TemperatureModifier.Add(0f, heat + reactorFudgeFactor * 50f);

            core.MaxCoolant = heat + reactorFudgeFactor * 50f;

            D_RealHeat = String.Format("{0:F2}",heat/50f + reactorFudgeFactor);

        }

        // track and set core damage
        private void HandleCoreDamage()
        {
          // Update reactor damage
          float critExceedance = (float)core.CoreTemperature - CriticalTemperature;

          // If overheated too much, damage the core
          if (critExceedance > 0f && TimeWarp.CurrentRate < 100f)
          {
              // core is damaged by Rate * temp exceedance * time
              CoreIntegrity = Mathf.MoveTowards(CoreIntegrity, 0f, CoreDamageRate * critExceedance * TimeWarp.fixedDeltaTime);
          }

          // Calculate percent exceedance of nominal temp
          float tempNetScale = 1f - Mathf.Clamp01((float)((core.CoreTemperature - NominalTemperature) / (MaximumTemperature - NominalTemperature)));


          if (OverheatAnimation != "")
          {
            for (int i = 0;i<overheatStates.Length;i++)

              {
                  overheatStates[i].normalizedTime = 1f - tempNetScale;
              }
          }
        }

        // Set ModuleResourceConverter ratios based on an input scale
        private void RecalculateRatios(float fuelInputScale)
        {

            for (int i = 0; i < _recipe.Inputs.Count; i++)
            {
                for (int j = 0; j < inputs.Count; j++)
                {
                    if (inputs[j].ResourceName == inputList[i].ResourceName)
                    {
                        _recipe.Inputs[i] = new ResourceRatio(inputList[i].ResourceName, inputs[j].ResourceRatio * fuelInputScale, inputList[i].DumpExcess);

                    }
                }
            }
            for (int i = 0; i < _recipe.Outputs.Count; i++)
            {
                for (int j = 0; j < outputs.Count; j++)
                {
                    if (outputs[j].ResourceName == outputList[i].ResourceName)
                    {
                        //Debug.Log("OUT: edited " + outputList[i].ResourceName + " ratio to " + (outputs[j].ResourceRatio * fuelInputScale).ToString());
                        _recipe.Outputs[i] = new ResourceRatio(outputList[i].ResourceName, inputs[j].ResourceRatio * fuelInputScale, outputList[i].DumpExcess);
                    }
                }
            }
            for (int i = 0; i < inputList.Count; i++)
            {
                //Debug.Log("IN: edited " + inputList[i].ResourceName + " ratio to " + (inputList[i].Ratio).ToString());
            }
        }



        // ####################################
        // Repairing
        // ####################################

        public bool TryRepairReactor()
        {
          if (CoreIntegrity <= MinRepairPercent)
          {
              ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_CoreTooDamaged"), 5.0f, ScreenMessageStyle.UPPER_CENTER));
              return false;
          }
          if (!CheckEVAEngineerLevel(EngineerLevelForRepair))
          {
              ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_CoreTooDamaged",EngineerLevelForRepair.ToString("F0")), 5.0f, ScreenMessageStyle.UPPER_CENTER));
              return false;
          }
          if (base.ModuleIsActive())
          {
              ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_NotWhileRunning"),
                  5.0f, ScreenMessageStyle.UPPER_CENTER));
              return false;
          }
          if (core.CoreTemperature > MaxTempForRepair)
          {
              ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_CoreTooHot", MaxTempForRepair.ToString("F0")), 5.0f, ScreenMessageStyle.UPPER_CENTER));
              return false;
          }
          if (CoreIntegrity >= MaxRepairPercent)
          {
              ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_CoreAlreadyRepaired", MaxRepairPercent.ToString("F0")),
                  5.0f, ScreenMessageStyle.UPPER_CENTER));
              return false;
          }
          return true;
        }

        // Repair the reactor to max Repair percent
        public void DoReactorRepair()
        {
            this.CoreIntegrity = MaxRepairPercent;
            ScreenMessages.PostScreenMessage(new ScreenMessage(Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Message_Repair_RepairSuccess", MaxRepairPercent.ToString("F0")), 5.0f, ScreenMessageStyle.UPPER_CENTER));
        }

        // Check the current EVA engineer's level
        private bool CheckEVAEngineerLevel(int level)
        {
            ProtoCrewMember kerbal = FlightGlobals.ActiveVessel.GetVesselCrew()[0];
            if (kerbal.experienceTrait.Title == "Engineer" && kerbal.experienceLevel >= level)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public float GetCoreTemperature()
        {
          return (float)core.CoreTemperature;
        }

        // ####################################
        // Refuelling
        // ####################################

        // Finds time remaining at specified fuel burn rates
        public string FindTimeRemaining(double amount, double rate)
        {
            if (rate < 0.0000001)
            {
                return Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus_VeryLong");
            }
            double remaining = amount / rate;
            //TimeSpan t = TimeSpan.FromSeconds(remaining);

            if (remaining >= 0)
            {
                return Utils.FormatTimeString(remaining);
            }
            {
                return Localizer.Format("#LOC_NFElectrical_ModuleFissionReactor_Field_FuelStatus_Exhausted");
            }
        }
    }
}
