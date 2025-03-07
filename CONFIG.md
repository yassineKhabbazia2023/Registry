# Variables d'environnement

## Exemple de fichier `app.settings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "BackGroundJob": {
    "TimeToWaitBeforeEachStep": 5,
    "Chunk": 1000,
    "ShouldTriggerEvents": true,
    "NumberOfDaysToRetryFailedRoles": -7
  },
  "DatabaseConnectionString": "*"
}
```

## Exemple de fichier `local.settings.json`

Dans un fichier `local.settings.json`, il faut remplacer les objets imbriqués par des tirets bas (`_`), comme dans l’exemple ci-dessous, pour la variable `BackGroundJob` :

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BackGroundJob__TimeToWaitBeforeEachStep": 5,
    "BackGroundJob__Chunk": 1000,
    "BackGroundJob__ShouldTriggerEvents": true,
    "BackGroundJob__NumberOfDaysToRetryFailedRoles": -7,
    "ConnectToResourcesViaManagedIdentity": false,
    "ConnectToBlobViaManagedIdentity": false
  }
}
```

## Important : exemples pour les variables `PullTopics` et `PushTopicName`

Certaines variables, telles que `PushTopicName` ou `PullTopics`, sont des objets contenant des listes de chaînes de caractères (strings). Pour les convertir en variables d’environnement, il faut ajouter un index pour chaque élément. L’exemple ci-dessous montre comment procéder dans le fichier `app.settings.json` :

```json
"BrokerSetting": {
  "FullyQualifiedNamespace": "Endpoint=sb://sbnscegpulsehubdev0301.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cACB7pgMG/Uoh85if6pk9zKBfkHTozc/S+ASbPR5L6k=",
  "ManagedIdentityClientId": "c9335b2f-7c66-4f2c-9850-8cd74d3921d0",
  "PushTopicName": [
    "registry"
  ],
  "PullTopics": [
    {
      "TopicName": "contact",
      "Subscriptions": [
        "contact-created-cnttest",
        "contact-updated-cnttest",
        "contact-removed-registry"
      ]
    },
    {
      "TopicName": "account",
      "Subscriptions": [
        "role-deleted-registry",
        "account-created-roletest",
        "account-created-acctest",
        "account-updated-acctest",
        "account-removed-acctest"
      ]
    }
  ]
}
```

Voici la transformation correspondante dans le `local.settings.json` (attention aux indices) :

```json
"BrokerSetting__ManagedIdentityClientId": "c9335b2f-7c66-4f2c-9850-8cd74d3921d0",
"BrokerSetting__PushTopicName__0": "registry",
"BrokerSetting__PullTopics__0__TopicName": "contact",
"BrokerSetting__PullTopics__0__Subscriptions__0": "contact-created-cnttest",
"BrokerSetting__PullTopics__0__Subscriptions__1": "contact-updated-cnttest",
"BrokerSetting__PullTopics__0__Subscriptions__2": "contact-removed-registry",
"BrokerSetting__PullTopics__1__TopicName": "account",
"BrokerSetting__PullTopics__1__Subscriptions__0": "role-deleted-registry",
"BrokerSetting__PullTopics__1__Subscriptions__1": "account-created-roletest",
"BrokerSetting__PullTopics__1__Subscriptions__2": "account-created-acctest",
"BrokerSetting__PullTopics__1__Subscriptions__3": "account-updated-acctest",
"BrokerSetting__PullTopics__1__Subscriptions__4": "account-removed-acctest"
```

Il est important de respecter les indices (`__0`, `__1`, etc.) pour refléter l’ordre et la structure de l’objet d’origine dans le `app.settings.json`.

[//]: # (SCRAP BELOW)
## Liste des variables d'environnement et leur effet

| Variable d’environnement                          | Description                                                                                                                                |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **BackGroundJob__TimeToWaitBeforeEachStep**       | Temps d’attente (en secondes) avant chaque étape d’un job en arrière-plan.                                                                 |
| **BackGroundJob__Chunk**                          | Taille du lot de données traité par itération.                                                                                             |
| **BackGroundJob__ShouldTriggerEvents**            | Indique si le job doit déclencher des événements.                                                                                          |
| **BackGroundJob__NumberOfDaysToRetryFailedRoles** | Nombre de jours (positif ou négatif) pendant lesquels on réessaye les rôles en échec.                                                      |
| **DatabaseConnectionString**                      | Chaîne de connexion à la base de données.                                                                                                  |
| **APPLICATIONINSIGHTS_CONNECTION_STRING**         | Chaîne de connexion Azure Application Insights (télémétrie et logs).                                                                       |
| **AllowedHosts**                                  | Liste d’hôtes autorisés (configuration ASP.NET Core).                                                                                      |
| **token**                                         | Le token utilisé pour appeler les endpoints d’injection de fichiers CSV (via Mulesoft). Il est stocké dans un Key Vault par environnement. |
| **RegistryApiUrl**                                | URL de l’API Registry.                                                                                                                     |
| **BlobStorageContainerName**                      | Nom du conteneur pour le stockage Blob.                                                                                                    |
| **ConnectToResourcesViaManagedIdentity**          | Indique si la connexion à différentes ressources (Service Bus, Key Vault, etc.) s’effectue via une identité managée (Managed Identity).    |
| **ConnectToBlobViaManagedIdentity**               | Indique si la connexion à Azure Blob s’effectue via une identité managée (Managed Identity).                                               |
| **BlobStoragePrimaryConnectionString**            | Chaîne de connexion principale pour le compte de stockage Blob.                                                                            |
| **BlobStorageUri**                                | URI du service Blob Storage (souvent utilisée localement via un émulateur).                                                                |
| **BrokerSetting__FullyQualifiedNamespace**        | Le namespace d’Azure Service Bus, pouvant inclure la chaîne de connexion complète permettant de se connecter au service.                   |
| **BrokerSetting__ManagedIdentityClientId**        | Le GUID de l’identité managée utilisée par le service « Registry ».                                                                        |
| **BrokerSetting__PushTopicName**                  | Le nom du topic sur lequel « Registry » va publier des événements.                                                                         |
| **BrokerSetting__PullTopics**                     | Le ou les topics sur lesquels « Registry » va écouter (s’abonner) pour recevoir des événements.                                            |
| **Referential__ClientId**                         | Client ID du référentiel utilisé pour un flux sortant (variable non utilisée actuellement).                                                |
| **Referential__ClientSecret**                     | Variable non utilisée actuellement.                                                                                                        |
| **Referential__CorrelationId**                    | Variable non utilisée actuellement.                                                                                                        |
| **Referential__Authorization**                    | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__TokenUrl**                    | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__GrantType**                   | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__ClientId**                    | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__ClientSecret**                | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__UserName**                    | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__Password**                    | Variable non utilisée actuellement.                                                                                                        |
| **ReferentialToken__Scope**                       | Variable non utilisée actuellement.                                                                                                        |
| **ProcessEventPublishBatchSize**                  | Taille du lot lors de la publication des événements.                                                                                       |
| **ServiceBusQueueProcessName**                    | Nom de la file d’attente Service Bus pour le traitement des messages.                                                                      |
| **ServiceBusTopicRegisteryName**                  | Nom du topic Service Bus (« registry ») où les messages sont publiés.                                                                      |