# Introductie Nieuwsbrief team Platform & Services

welkom bij de eerste nieuwsbrief van het team Platform & Services! Hoewel het team al geruime tijd bestaat hebben we nog geen reviews gehouden. Dit heeft voor een groot deel te maken met het feit dat we veelal erg technische onderwerpen behandelen die niet altijd even toegankelijk zijn voor een breder publiek. Met deze nieuwsbrief willen we jullie op de hoogte houden van onze activiteiten, projecten en andere relevante informatie. Wat kunnen jullie concreet verwachten?

- Een korte samenvatting van de belangrijkste activiteiten van het team
- Aankondigingen / calls to action
- Post Mortems
- Architectural Decision Records (ADRs)
- Informatie over de roadmap van het team

Het is ons streven om deze nieuwsbrief maandelijks te versturen. We hopen dat jullie het een waardevolle aanvulling vinden en staan open voor feedback en suggesties. Mochten jullie vragen hebben, aarzel dan niet om contact met ons op te nemen.


# Aankondigingen/CTA's
In deze sectie vind je aankondigingen en calls to action van het team Platform & Services. We moedigen iedereen aan om actief deel te nemen en feedback te geven op de verschillende onderwerpen. 

## keyvault rbac roles

In een artikel van Microsoft https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-access-policy wordt uitgelegd hoe je Key Vault kunt beveiligen met RBAC-rollen in plaats van toegangspolicies. Dit biedt meer flexibiliteit en controle over wie toegang heeft tot de Key Vault en welke acties ze kunnen uitvoeren. Ook in de vraag https://learn.microsoft.com/en-us/answers/questions/1691908/access-policies-for-keyvaults-decommission wordt aangegeven dat access policies geen einddatum hebben, maar wel als legacy worden beschouwd.
Om te laten zien welke stappen nodig zijn. De mapping van je huidige policies naar RBAC-rollen is te vinden in de volgende link:https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-migration?tabs=cli#access-policies-to-azure-roles-mapping. Bijvoorbeeld deze mapping:

```bicep
resource accessForSP 'accessPolicies' = {
    name: 'add'
    properties: {
      accessPolicies: [
        {
          objectId: identity.properties.principalId
          tenantId: identity.properties.tenantId
          permissions: {
            secrets: [
              'get'
              'list'
            ]
            keys: [
              'get'
              'wrapKey'
              'unwrapKey'
            ]
          }
        }
      ]
    }
```

wordt dan:

```bicep

var keyVaultSecretUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultCryptoUserRoleId = '12338af0-0e69-4776-bea7-57ae8d297424'

resource identityKeyVaultSecretsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, keyVault.id, identity.id, 'secretsuser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretUserRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource identityKeyVaultCryptoRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, keyVault.id, identity.id, 'cryptouser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCryptoUserRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
```

Dit deployen naar de keyvault werkt gewoon. Verder set je de `enableRbacAuthorization: true` in de Key Vault resource definitie. 
Echter, De echte overstap gebeurd pas *na* dat je in de portal zelf de instelling aanpast (onder Settings => Access configuration). Daar staat het permission model voor velen op dit moment op 'Vault access policy' en moet je dit aanpassen naar 'Azure role-based access control (RBAC)'. Dit is een handmatige actie die je moet uitvoeren. Voor productie heb je daar een PIM verzoek voor nodig.
Let op dat wanneer je dit doet, de resources die dat benaderen herstart, zodat die er gebruik van maken. 
Let voor test op dat je calls naar keyvault niet meer werken, omdat de policies niet meer bestaan. Je moet dan de RBAC rollen toekennen aan de service principals die de Key Vault benaderen.