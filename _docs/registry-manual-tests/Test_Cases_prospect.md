AC:

## Cas de test  
  
### 1. Validation du type de compte  
  
#### Cas 1.1 — `CLIENT` avec flag activé  [TEST OK ✅] 
**Données :**  
- ligne CSV `Account`  
- `accountType = CLIENT`  

```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
1;Registry Client Insert 001;9999990061;CLIENT INSERT AUTO 001;CLIENT;client.insert001@example.com;6201Z;Programmation informatique;;;;;Akuiteo;;82300000000161;;;;;;;SAS - SOCIÉTÉ PAR ACTIONS SIMPLIFIÉE;;Personne morale;;2025-02-06T09:14:01.527;2025-02-06T10:11:23.733;10 RUE DE LA PAIX;;;PARIS;75002;France;;10 RUE DE LA PAIX;;;PARIS;75002;France;;0102030405;0102030405;INSERT
```
  
**Résultat attendu :**  
- traitement accepté  
- aucun changement de comportement par rapport à aujourd’hui
  
#### Cas 1.2 — `PROSPECT` avec flag désactivé   [TEST OK ✅]
**Données :**  
- ligne CSV `Account`  
- `accountType = PROSPECT`  
```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"PROSPECT TEST 001";"9010018178";"PROSPECT AUTO 001";"PROSPECT";"";"5610C";"Restauration";"";"";"";"";"Akuiteo";"";"99346535000012";"";"";"";"";"";"";"";"";"Personne morale";"";"2026-04-08T09:44:21.617";"2026-04-08T10:04:31.637";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"";"";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"France";"";"";"";"INSERT"
```

**Résultat attendu :**  
- traitement refusé  
- comportement identique à l’existant avant implémentation  

  
#### Cas 1.3 — `PROSPECT` avec flag activé  [TEST OK ✅]
**Données :**  
- ligne CSV `Account`  
- `accountType = PROSPECT`  
- 
```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"PROSPECT TEST 001";"9010018178";"PROSPECT AUTO 001";"PROSPECT";"";"5610C";"Restauration";"";"";"";"";"Akuiteo";"";"99346535000012";"";"";"";"";"";"";"";"";"Personne morale";"";"2026-04-08T09:44:21.617";"2026-04-08T10:04:31.637";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"";"";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"France";"";"";"";"INSERT"
```

**Résultat attendu :**  
- traitement accepté  
- Ligne account ajouter dans le spoke acccount et registry et prospect
- Pas de ligne ajouté dans le spoke pennylane et offer


---  
  
### 2. Consommation CSV — INSERT account PROSPECT  
   
#### Cas 2.1 — INSERT PROSPECT sur un prospect déjà existant  [TEST OK ✅]
**Données :**  
- `operation = INSERT`  
- `accountType = PROSPECT`  
- account déjà existant
-   
```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"PROSPECT TEST 001";"9010018178";"PROSPECT AUTO 001";"PROSPECT";"";"5610C";"Restauration";"";"";"";"";"Akuiteo";"";"99346535000012";"";"";"";"";"";"";"";"";"Personne morale";"";"2026-04-08T09:44:21.617";"2026-04-08T10:04:31.637";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"";"";"51 RUE DE SAINT-CYR";"";"";"LYON";"69009";"France";"";"";"";"INSERT"
```

**Résultat attendu :**  
- traitement comme un `UPDATE`  
- aucune duplication de l’account  
- données mises à jour correctement  
- émission de l’événement attendue avec `AccountType = PROSPECT`  
  
---  
  
### 3. Consommation CSV — UPDATE account PROSPECT  
  
#### Cas 3.1 — UPDATE PROSPECT sur un prospect inexistant  [TEST OK ✅]
**Données :**  
- `operation = UPDATE`  
- `accountType = PROSPECT`  
- account inexistant 
-  
```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"TEST PROSPECT UPDATE";"9010099999";"TEST PROSPECT UPDATE";"PROSPECT";"prospect.update@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456789";"10";"";"";"150000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"0102030405";"0102030405";"UPDATE"
```

**Résultat attendu :**  
- insertion du prospect  
- aucune erreur bloquante  
- émission de l’événement attendue avec `AccountType = PROSPECT`  
- iso **`Cas 1.3`**
  
#### Cas 3.2 — UPDATE PROSPECT sur un prospect existant  [TEST OK ✅]
**Données :**  
- `operation = UPDATE`  
- `accountType = PROSPECT`  
- account existant  

```csv
AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"TEST PROSPECT UPDATE";"9010099999";"TEST PROSPECT UPDATE";"PROSPECT";"prospect.update@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456789";"10";"";"";"150000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"0102030405";"0102030405";"UPDATE"
```

**Résultat attendu :**  
- mise à jour du prospect  
- comportement cohérent avec les règles attendues  
- émission de l’événement attendue avec `AccountType = PROSPECT`  
  
---  
  
### 4. Consommation CSV — DELETE account PROSPECT  [⛔ SKIPPED: flux delete dans ergistry n'est pas active dans registry + pas stable suite a pas mal des évolutions sur les règles métier du projet ⛔ ]
  
#### Cas 4.1 — DELETE PROSPECT  
**Données :**  
- `operation = DELETE`  
- `accountType = PROSPECT`  
  
**Résultat attendu :**  
- comportement iso-fonctionnel à la suppression d’un `CLIENT`  
- aucune règle spécifique supplémentaire introduite  
- aucune régression sur le flow existant  
  
---  
  
### 5. Consommation CSV — rôles liés à un account PROSPECT  [A TESTER: on peut le tester que avec la version déployé 🔄 ]
  
#### Cas 5.1 — INSERT role lié à un PROSPECT  
**Données :**  
- ligne CSV `Role`  
- `operation = INSERT`  
- rôle rattaché à un account `PROSPECT`  
```csv
Account 

AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"PROSPECT TEST CLIENT ROLE";"9010099988";"PROSPECT TEST";"PROSPECT";"prospect.test@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456789";"10";"";"";"150000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"0102030405";"0102030405";"INSERT"

Contact
ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation
1;test+prospect-role@test.onmicrosoft.com;ARTHUR;DEBRIEL;1;;;;;INSERT

Role
ContactEmail;AccountNumber;ContactFlagPortailFactures;ContactFlagMainContact;RoleFlagStatus;Description;Operation
"test+prospect-role@test.onmicrosoft.com";"9010099988";"true";"true";"0";"Co dirigeant";"INSERT"
```
**Résultat attendu :**  
- rôle traité correctement  
- comportement iso à un rôle lié à un `CLIENT`  
- Vérifier que le rôle est présent dans la base account
- Vérifier que le rôle est présent dans la base prospect
- Vérifier que le rôle n'est pas présent dans la base Pennylane (vérifier également les logs App Insights)

  
#### Cas 5.2 — CHANGE EMAIL sur contact lié à un PROSPECT
**Objectif :**  
vérifier qu’un changement d’email sur un contact rattaché à un account `PROSPECT` est bien traité, et que le rôle déjà existant est bien rattaché au nouvel email.

##### Étape 1 — créer le PROSPECT + contact initial + rôle
```csv
Account

AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"PROSPECT TEST CHANGE EMAIL";"9010099989";"PROSPECT CHANGE EMAIL";"PROSPECT";"prospect.change.email@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456780";"10";"";"";"150000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"0102030405";"0102030405";"INSERT"

Contact

ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation
1;test+prospect-old-email@test.onmicrosoft.com;ARTHUR;DEBRIEL;1;0102030405;;;;INSERT

Role

ContactEmail;AccountNumber;ContactFlagPortailFactures;ContactFlagMainContact;RoleFlagStatus;Description;Operation
"test+prospect-old-email@test.onmicrosoft.com";"9010099989";"true";"true";"0";"Co dirigeant";"INSERT"
```

##### Étape 2 — simuler le change email
Ici on envoie un `DELETE` de l’ancien contact et un `INSERT` du nouveau contact.

```csv
Contact

ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation
1;test+prospect-old-email@test.onmicrosoft.com;ARTHUR;DEBRIEL;1;0102030405;;;;DELETE
1;test+prospect-new-email@test.onmicrosoft.com;ARTHUR;DEBRIEL;1;0102030405;;;;INSERT
```

##### Résultat attendu
- le change email est détecté correctement
- les opérations `CONTACT DELETE` + `CONTACT INSERT` sont remplacées par une opération `CONTACT UPDATE`
- l’opération `UPDATE` contient `OldContactEmail = test+prospect-old-email@test.onmicrosoft.com`
- le rôle déjà existant sur le `PROSPECT` est rattaché au nouvel email `test+prospect-new-email@test.onmicrosoft.com`
- comportement iso à un contact lié à un `CLIENT`

##### Vérifications à faire
- vérifier dans reg.Operations qu’on a un UPDATE CONTACT et non plus le couple DELETE / INSERT
- vérifier dans la base Registry que le contact porte le nouvel email
- vérifier dans la table des rôles que le rôle est bien lié à test+prospect-new-email@test.onmicrosoft.com
- vérifier dans la base prospect que la relation est cohérente
- vérifier les logs / App Insights autour du traitement ReviewChangeEmail
---  
  
### 6. Non-régression sur CLIENT  [A TESTER: déploiement nécessaire 🔄]
  
#### Cas 6.1 — INSERT CLIENT 
**Données :**
- ligne CSV `Account`
- `operation = INSERT`
- `account type = CLIENT`
- feature flag Prospect activé ou désactivé, sans impact attendu sur ce cas
  
```csv 
Account

AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"CLIENT TEST INSERT";"9010099990";"CLIENT TEST";"CLIENT";"client.test.insert@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456781";"10";"";"";"150000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"10 Rue de la Paix";"";"";"Paris";"75002";"FR";"";"0102030405";"0102030405";"INSERT"
```
**Résultat attendu :**  
- account CLIENT accepté comme avant
- création traitée normalement
- aucun changement de comportement lié à l’ajout du support PROSPECT
- le feature flag Prospect n’a aucun impact sur ce cas

**Vérifications à faire :**
- vérifier que la ligne passe la light validation
- vérifier qu’une opération account est bien créée en base
- vérifier que l’account est bien présent dans la base Registry
- vérifier que l’event account est bien publié comme avant
- vérifier dans les logs / App Insights qu’aucun rejet lié au feature flag Prospect n’apparaît
- vérifier qu’il n’y a pas de différence de comportement entre flag Prospect activé et désactivé 
  
#### Cas 6.2 — UPDATE CLIENT

**Données :**
- ligne CSV `Account`
- `operation = UPDATE`
- `account type = CLIENT`
- account CLIENT déjà existant dans Registry
- feature flag Prospect activé ou désactivé, sans impact attendu sur ce cas
  
```csv
Account

AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"CLIENT TEST UPDATE";"9010099991";"CLIENT TEST UPDATED";"CLIENT";"client.test.update@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456782";"20";"";"";"250000";"";"";"SAS";"20-49";"";"5710";"2026-04-30";"2026-04-30";"20 Rue de la Republique";"";"";"Lyon";"69002";"FR";"";"20 Rue de la Republique";"";"";"Lyon";"69002";"FR";"";"0102030406";"0102030406";"UPDATE"
```

**Résultat attendu :**
- account CLIENT accepté comme avant
- mise à jour traitée normalement
- aucun changement de comportement lié à l’ajout du support PROSPECT
- le feature flag Prospect n’a aucun impact sur ce cas
  
**Vérifications à faire :**
- vérifier que la ligne passe la light validation
- vérifier qu’une opération account de mise à jour est bien créée ou traitée comme avant
- vérifier que l’account est bien mis à jour dans la base Registry
- vérifier que l’event account update est bien publié comme avant
- vérifier dans les logs / App Insights qu’aucun rejet lié au feature flag Prospect n’apparaît
- vérifier qu’il n’y a pas de différence de comportement entre flag Prospect activé et désactivé
---
#### Cas 6.3 — INSERT role lié à CLIENT
**Données :**
- ligne CSV `Role`
- `operation = INSERT`
- rôle rattaché à un account `CLIENT`
- feature flag Prospect activé ou désactivé, sans impact attendu sur ce cas

```csv
Account

AccountFlagStatus;LegalName;AccountNumber;AccountCommercialName;AccountType;AccountEmail;AccountNafIdentifier;AccountSectorCode;AccountTaxeValeurAjoutee;AccountDeliveryEmail;AccountBillingEmail;AccountTaxationSystem;AccountSourceName;AccountISIN;AccountRegisterIdentification1;AccountStaffSize;AccountDeliveryFax;AccountBillingFax;AccountTurnover;AccountRegimeFiscal;AccountTypeTenueComptable;AccountFormeJuridique;AccountStaffSizeSlice;AccountEscCategory;AccountCodeFormeJuridique;AccountInsertedDate;AccountUpdatedDate;DeliveryAddressLine1;DeliveryAddressLine2;DeliveryAddressLine3;DeliveryCity;DeliveryZipCode;DeliveryCountry;DeliveryState;BillingAddressLine1;BillingAddressLine2;BillingAddressLine3;BillingCity;BillingZipCode;BillingCountry;BillingState;AccountDeliveryPhone;AccountBillingPhone;Operation
"1";"CLIENT TEST ROLE";"9010099992";"CLIENT ROLE TEST";"CLIENT";"client.test.role@test.fr";"6201Z";"Programmation informatique";"";"";"";"";"Akuiteo";"";"123456783";"10";"";"";"180000";"";"";"SAS";"10-19";"";"5710";"2026-04-30";"2026-04-30";"30 Rue Victor Hugo";"";"";"Marseille";"13001";"FR";"";"30 Rue Victor Hugo";"";"";"Marseille";"13001";"FR";"";"0102030407";"0102030407";"INSERT"

Contact

ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeId;Operation
1;test+client-role@test.onmicrosoft.com;ARTHUR;DEBRIEL;1;;;;;INSERT

Role

ContactEmail;AccountNumber;ContactFlagPortailFactures;ContactFlagMainContact;RoleFlagStatus;Description;Operation
"test+client-role@test.onmicrosoft.com";"9010099992";"true";"true";"0";"Co dirigeant";"INSERT"
```

**Résultat attendu :**
- rôle traité correctement
- comportement inchangé par rapport à l’existant
- aucun impact du support PROSPECT sur ce scénario
- le feature flag Prospect n’a aucun impact sur ce cas

**Vérifications à faire :**
- vérifier que les lignes passent la validation
- vérifier que le contact est bien créé / disponible dans Registry
- vérifier que le rôle est bien créé pour l’account CLIENT
- vérifier que le rôle est bien présent dans la base account
- vérifier que le comportement observé est identique avec flag Prospect activé ou désactivé
- vérifier dans les logs / App Insights qu’aucun rejet lié au feature flag Prospect n’apparaît
  
---  
  
### 7. Vérification du feature flag  
  
#### Cas 7.1 — Activation du flag  [TEST OK ✅]
**Résultat attendu :**  
- les flows `PROSPECT` sont pris en charge  
  
#### Cas 7.2 — Désactivation du flag  [TEST OK ✅]
**Résultat attendu :**  
- retour immédiat au comportement historique  
- les flows `PROSPECT` ne sont plus acceptés  
- les flows `CLIENT` restent inchangés
