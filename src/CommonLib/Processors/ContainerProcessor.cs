﻿using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using SharpHoundCommonLib.LDAPQueries;
using SharpHoundCommonLib.OutputTypes;

namespace SharpHoundCommonLib.Processors
{
    public class ContainerProcessor
    {
        /// <summary>
        /// Finds all immediate child objects of a container. 
        /// </summary>
        /// <param name="distinguishedName"></param>
        /// <returns></returns>
        public static IEnumerable<TypedPrincipal> GetContainerChildObjects(string distinguishedName)
        {
            var filter = new LDAPFilter().AddComputers().AddUsers().AddGroups().AddOUs().AddContainers();
            var utils = LDAPUtils.Instance;
            foreach (var childEntry in utils.QueryLDAP(filter.GetFilter(), SearchScope.OneLevel,
                CommonProperties.ObjectID, Helpers.DistinguishedNameToDomain(distinguishedName), adsPath: distinguishedName))
            {
                var dn = childEntry.DistinguishedName.ToUpper();
                
                if (dn.Contains("CN=SYSTEM") || dn.Contains("CN=POLICIES") || dn.Contains("CN=PROGRAM DATA"))
                    continue;

                var id = childEntry.GetObjectIdentifier();
                if (id == null)
                    continue;

                var res = utils.ResolveIDAndType(id, Helpers.DistinguishedNameToDomain(childEntry.DistinguishedName));
                if (res == null)
                    continue;
                yield return res;
            }
        }

        /// <summary>
        /// Reads the "gplink" property from a SearchResult and converts the links into the acceptable SharpHound format
        /// </summary>
        /// <param name="gpLink"></param>
        /// <returns></returns>
        public static IEnumerable<GPLink> ReadContainerGPLinks(string gpLink)
        {
            if (gpLink == null)
                yield break;

            foreach (var link in Helpers.SplitGPLinkProperty(gpLink))
            {
                var enforced = link.Status.Equals("2");

                var res = LDAPUtils.Instance.ResolveDistinguishedName(link.DistinguishedName);
                    
                if (res == null)
                    continue;

                yield return new GPLink
                {
                    GUID = res.ObjectIdentifier,
                    IsEnforced = enforced
                };
            }
        }
        
        /// <summary>
        /// Checks if a container blocks privilege inheritance
        /// </summary>
        /// <param name="gpOptions"></param>
        /// <returns></returns>
        public static bool ReadBlocksInheritance(string gpOptions)
        {
            return gpOptions is "1";
        }
    }
}