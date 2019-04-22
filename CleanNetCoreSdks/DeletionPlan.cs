using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Austin.CleanNetCoreSdks
{
    public class DeletionPlan
    {
        public DeletionPlan(bool keepOnlyLastVersionPerRuntime, List<DotNetCoreSdk> installedSdks, HashSet<SdkVersion> visualStudioVersions)
        {
            SdksToDelete = new List<DotNetCoreSdk>();
            SdksToKeep = new List<DotNetCoreSdk>();
            SdksPinnedByVisualStudio = new List<DotNetCoreSdk>();
            var visualStudioBands = new HashSet<SdkVersion>(visualStudioVersions.Select(v => v.SdkVersionBand));
            CalculateKeep(keepOnlyLastVersionPerRuntime, installedSdks.Where(sdk => sdk.Is64Bit), visualStudioVersions, visualStudioBands);
            CalculateKeep(keepOnlyLastVersionPerRuntime, installedSdks.Where(sdk => !sdk.Is64Bit), visualStudioVersions, visualStudioBands);
        }

        void CalculateKeep(bool keepOnlyLastVersionPerRuntime, IEnumerable<DotNetCoreSdk> installedSdksEnumerable, HashSet<SdkVersion> visualStudioVersions, HashSet<SdkVersion> visualStudioBands)
        {
            DotNetCoreSdk previous = null;
            foreach (var sdk in installedSdksEnumerable.OrderByDescending(s => s.Version))
            {
                if (previous == null)
                {
                    //Always keep the latest version.
                    SdksToKeep.Add(sdk);
                }
                else if (keepOnlyLastVersionPerRuntime && !previous.Version.IncludedRuntimeBand.Equals(sdk.Version.IncludedRuntimeBand))
                {
                    SdksToKeep.Add(sdk);
                }
                else if (!keepOnlyLastVersionPerRuntime && !previous.Version.SdkVersionBand.Equals(sdk.Version.SdkVersionBand))
                {
                    SdksToKeep.Add(sdk);
                }
                else //By the normal rules we do not want to keep this SDK, but Viusal Studio might want it.
                {
                    if (visualStudioVersions.Contains(sdk.Version))
                    {
                        //Keep exact matching VS version in case Visual studio does not like it getting pulled out from under it.
                        SdksToKeep.Add(sdk);
                        SdksPinnedByVisualStudio.Add(sdk);
                    }
                    else if (visualStudioBands.Contains(sdk.Version.SdkVersionBand) && !previous.Version.SdkVersionBand.Equals(sdk.Version.SdkVersionBand))
                    {
                        //Keep the newest version in the version band that Visual Studio desires.
                        SdksToKeep.Add(sdk);
                        SdksPinnedByVisualStudio.Add(sdk);
                    }
                    else
                    {
                        SdksToDelete.Add(sdk);
                    }
                }

                previous = sdk;
            }
        }

        public List<DotNetCoreSdk> SdksToDelete { get; }
        public List<DotNetCoreSdk> SdksToKeep { get; }
        public List<DotNetCoreSdk> SdksPinnedByVisualStudio { get; }
    }
}
