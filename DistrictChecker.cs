using System;
using UnityEngine;
using ColossalFramework;

namespace GSteigertDistricts
{
    internal static class DistrictChecker
    {
        private static bool IsTransferAllowed(Vector3 source, Vector3 destination,
            ushort buildingID, TransferManager.TransferReason reason, bool isResidentTransfer)
        {
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            byte srcDistrict = districtManager.GetDistrict(source);
            byte dstDistrict = districtManager.GetDistrict(destination);

            // check whether this transfer represents a resident going out to do something
            if (isResidentTransfer)
            {
                switch (reason)
                {
                    // resident going to a hospital or clinic
                    case TransferManager.TransferReason.Sick:
                        return Settings.RestrictCitizenHealthAccess ?
                            (dstDistrict == 0 || srcDistrict == dstDistrict) : true;

                    // resident going to an educational building
                    case TransferManager.TransferReason.Student1:
                    case TransferManager.TransferReason.Student2:
                    case TransferManager.TransferReason.Student3:
                        return Settings.RestrictCitizenEducationalAccess ?
                            (dstDistrict == 0 || srcDistrict == dstDistrict) : true;

                    // resident going to a park
                    case TransferManager.TransferReason.Entertainment:
                    case TransferManager.TransferReason.EntertainmentB:
                    case TransferManager.TransferReason.EntertainmentC:
                    case TransferManager.TransferReason.EntertainmentD:
                        return Settings.RestrictCitizenParkAccess ?
                            (dstDistrict == 0 || srcDistrict == dstDistrict) : true;

                    // resident going to a shop
                    case TransferManager.TransferReason.Shopping:
                    case TransferManager.TransferReason.ShoppingB:
                    case TransferManager.TransferReason.ShoppingC:
                    case TransferManager.TransferReason.ShoppingD:
                    case TransferManager.TransferReason.ShoppingE:
                    case TransferManager.TransferReason.ShoppingF:
                    case TransferManager.TransferReason.ShoppingG:
                    case TransferManager.TransferReason.ShoppingH:
                        return Settings.RestrictCitizenShoppingAccess ?
                            (dstDistrict == 0 || srcDistrict == dstDistrict) : true;

                    // resident going to work
                    case TransferManager.TransferReason.Worker0:
                    case TransferManager.TransferReason.Worker1:
                    case TransferManager.TransferReason.Worker2:
                    case TransferManager.TransferReason.Worker3:
                        return Settings.RestrictCitizenWorkAccess ?
                            (dstDistrict == 0 || srcDistrict == dstDistrict) : true;

                    default:
                        return true;
                }
            }
            else
            {
                ServiceBuildingOptions opts = ServiceBuildingOptions.GetInstance();
                switch (reason)
                {
                    // vehicle fetching something
                    case TransferManager.TransferReason.Garbage:
                    case TransferManager.TransferReason.Crime:
                    case TransferManager.TransferReason.Sick:
                    case TransferManager.TransferReason.Sick2:
                    case TransferManager.TransferReason.Dead:
                    case TransferManager.TransferReason.Fire:
                    case TransferManager.TransferReason.Fire2:
                    case TransferManager.TransferReason.Taxi:
                    case TransferManager.TransferReason.Snow:
                    case TransferManager.TransferReason.RoadMaintenance:
                        return !Settings.RestrictServiceDispatching ? true :
                            (srcDistrict == 0
                                || srcDistrict == dstDistrict
                                || opts.IsTargetCovered(buildingID, dstDistrict));

                    // vehicle freeing building capacity
                    case TransferManager.TransferReason.DeadMove:
                    case TransferManager.TransferReason.GarbageMove:
                    case TransferManager.TransferReason.SnowMove:
                        return !Settings.RestrictMaterialTransfer ? true :
                            (dstDistrict == 0
                                || srcDistrict == dstDistrict
                                || opts.IsTargetCovered(buildingID, dstDistrict));

                    // ignore prisons
                    case TransferManager.TransferReason.CriminalMove:
                        return true;

                    default:
                        return true;
                }
            }
        }

        public static bool IsBuildingTransferAllowed(ushort buildingID, ref Building data,
            TransferManager.TransferReason reason, TransferManager.TransferOffer offer,
            bool summarizedLog = false)
        {
            bool allowed = DistrictChecker.IsTransferAllowed(data.m_position, offer.Position, buildingID, reason, false);

#if DEBUG
            if (summarizedLog)
            {
                Utils.LogBuilding(String.Format("   - Building #{0} queried (allowed: {1})", buildingID, allowed));
            }
            else
            {
                DistrictManager districtManager = Singleton<DistrictManager>.instance;
                BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                CitizenManager citizenManager = Singleton<CitizenManager>.instance;

                string srcBuilding = buildingManager.GetBuildingName(buildingID, InstanceID.Empty);
                string dstBuilding = buildingManager.GetBuildingName(offer.Building, InstanceID.Empty);
                string dstCitizen = citizenManager.GetCitizenName(offer.Citizen);
                string srcDistrict = FindDistrictName(data.m_position);
                string dstDistrict = FindDistrictName(offer.Position);

                Utils.LogBuilding("------------------------------------------------------------"
                    + String.Format("\nBuilding #{0} queried (allowed: {1})", buildingID, allowed)
                    + String.Format("\n - Transfer: reason={0}, offer={1}", reason, Utils.ToString(offer))
                    + String.Format("\n - Origin: {0}", srcBuilding)
                    + String.Format("\n - Destination: building='{0}', citizen='{1}'", dstBuilding, dstCitizen)
                    + String.Format("\n - District: {0} -> {1}", srcDistrict, dstDistrict));
            }
#endif

            return allowed;
        }

        public static bool IsVehicleTransferAllowed(ushort vehicleID, ref Vehicle data,
            TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            ushort buildingID = data.m_sourceBuilding;
            Building building = buildingManager.m_buildings.m_buffer[(int)buildingID];
            bool allowed = DistrictChecker.IsTransferAllowed(building.m_position, offer.Position, buildingID, reason, false);

#if DEBUG
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            string srcBuilding = buildingManager.GetBuildingName(data.m_sourceBuilding, InstanceID.Empty);
            string dstBuilding = buildingManager.GetBuildingName(offer.Building, InstanceID.Empty);
            string dstCitizen = citizenManager.GetCitizenName(offer.Citizen);
            string srcDistrict = FindDistrictName(building.m_position);
            string dstDistrict = FindDistrictName(offer.Position);

            Utils.LogVehicle("------------------------------------------------------------"
                + String.Format("\nVehicle #{0} queried (allowed: {1})", vehicleID, allowed)
                + String.Format("\n - Transfer: reason={0}, offer={1}", reason, Utils.ToString(offer))
                + String.Format("\n - Origin: {0}", srcBuilding)
                + String.Format("\n - Destination: building='{0}', citizen='{1}'", dstBuilding, dstCitizen)
                + String.Format("\n - District: {0} -> {1}", srcDistrict, dstDistrict));
#endif

            return allowed;
        }

        public static bool IsCitizenTransferAllowed(uint citizenID, ref Citizen data,
            TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[(int)data.m_homeBuilding];
            bool allowed = DistrictChecker.IsTransferAllowed(building.m_position, offer.Position, 0, reason, true);

#if DEBUG
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;

            string srcBuilding = buildingManager.GetBuildingName(data.m_homeBuilding, InstanceID.Empty);
            string dstBuilding = buildingManager.GetBuildingName(offer.Building, InstanceID.Empty);
            string dstCitizen = citizenManager.GetCitizenName(offer.Citizen);
            string srcDistrict = FindDistrictName(building.m_position);
            string dstDistrict = FindDistrictName(offer.Position);

            Utils.LogCitizen("------------------------------------------------------------"
                + String.Format("\nCitizen #{0} queried (allowed: {1})", citizenID, allowed)
                + String.Format("\n - Transfer: reason={0}, offer={1}", reason, Utils.ToString(offer))
                + String.Format("\n - Origin: {0}", srcBuilding)
                + String.Format("\n - Destination: building='{0}', citizen='{1}'", dstBuilding, dstCitizen)
                + String.Format("\n - District: {0} -> {1}", srcDistrict, dstDistrict));
#endif

            return allowed;
        }

        private static string FindDistrictName(Vector3 position)
        {
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            byte districtId = districtManager.GetDistrict(position);
            string districtName = districtManager.GetDistrictName(districtId);
            return (districtName == null ? "<undefined>" : districtName) + ":" + districtId;
        }

        public static bool IsActive(byte districtID)
        {
            if (districtID == 0)
            {
                return false;
            }

            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            if (districtManager.m_districts.m_buffer[districtID].m_flags == District.Flags.None)
            {
                return false;
            }

            return true;
        }
    }
}
