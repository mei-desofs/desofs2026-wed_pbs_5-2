# Mapeamento ASVS ↔ Testes — LawyerApp

---

## Índice

- [V1.2.4 – Prevenção de Injecção SQL](#v124--prevenção-de-injecção-sql)
- [V2.2.1 – Validação de Input](#v221--validação-de-input)
- [V2.3.1 / V2.3.2 / V2.3.3 – Lógica de Negócio](#v231--v232--v233--lógica-de-negócio)
- [V4.1.1 / V4.1.4 – API e Serviços Web](#v411--v414--api-e-serviços-web)
- [V5.1.1 / V5.2.2 / V5.3.2 – Gestão de Ficheiros](#v511--v522--v532--gestão-de-ficheiros)
- [V6.2.8 / V6.3.2 / V6.3.8 – Autenticação](#v628--v632--v638--autenticação)
- [V7.2.1 / V7.2.2 – Gestão de Sessão](#v721--v722--gestão-de-sessão)
- [V8.2.1 / V8.2.2 / V8.3.1 – Autorização](#v821--v822--v831--autorização)
- [V9.1.1 / V9.1.2 / V9.1.3 / V9.2.1 / V9.2.3 – Tokens Auto-contidos](#v911--v912--v913--v921--v923--tokens-auto-contidos)
- [V11.2.3 / V11.3.1 / V11.4.1 / V11.4.2 – Criptografia](#v1123--v1131--v1141--v1142--criptografia)
- [V13.4.2 / V13.4.4 – Configuração](#v1342--v1344--configuração)
- [V14.2.1 – Protecção de Dados](#v1421--protecção-de-dados)
- [V16 – Logging e Tratamento de Erros](#v16--logging-e-tratamento-de-erros)

---

## V1.2.4 – Prevenção de Injecção SQL

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V1.2.4** | Queries a bases de dados usam ORM / queries parametrizadas (prevenção de SQL Injection) | Coberto | [`V1_2_4_GetByEmail_WithSqlInjectionPayload_ReturnsNull_NotAllUsers` *(5 variantes)*](LawyerApp.Tests/Unit/Security/ASVS/V1_2_4_SqlInjectionPreventionTests.cs#L39) · [`V1_2_4_EmailExists_WithSqlInjectionPayload_ReturnsFalse` *(2 variantes)*](LawyerApp.Tests/Unit/Security/ASVS/V1_2_4_SqlInjectionPreventionTests.cs#L57) · [`V1_2_4_GetByStoredFileName_WithMaliciousPayload_ReturnsNull` *(3 variantes)*](LawyerApp.Tests/Unit/Security/ASVS/V1_2_4_SqlInjectionPreventionTests.cs#L75) · [`V1_2_4_GetAllClients_DoesNotAcceptUserControlledFilter`](LawyerApp.Tests/Unit/Security/ASVS/V1_2_4_SqlInjectionPreventionTests.cs#L86) |

---

## V2.2.1 – Validação de Input

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V2.2.1** | Input validado contra estrutura esperada; email duplicado rejeitado; controlo de acesso por relação | Coberto | [`V2_2_1_CreateClient_WithDuplicateEmail_IsRejected`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L43) · [`V2_2_1_CreateClient_WithUniqueEmail_IsAccepted`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L56) · [`V2_2_1_UserHasAccess_IsEnforced_ByProcessRelation`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L152) · [`CreateClientAsync_WhenEmailAlreadyExists_ReturnsFailure`](LawyerApp.Tests/Unit/Services/ClientServiceTests.cs#L52) |

---

## V2.3.1 / V2.3.2 / V2.3.3 – Lógica de Negócio

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V2.3.1** | Fluxos de lógica de negócio processados na ordem correcta sem saltar passos | Coberto | [`V2_3_1_EmailCheck_IsPerformed_BeforePersistence`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L75) |
| **V2.3.2** | Limites de lógica de negócio aplicados; estado inicial do processo | Coberto | [`V2_3_2_LegalProcess_InitialStatus_MustBeOpen`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L103) · [`V2_3_2_LegalProcess_StatusTransition_IsApplied_Atomically`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L115) · [`LegalProcess_InitialStatus_IsOpen`](LawyerApp.Tests/Unit/Domain/ClientTests.cs#L57) |
| **V2.3.3** | Operações em múltiplos stores são atómicas | Coberto | [`V2_3_3_DeleteProcess_OnlyRemovesTargetProcess`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L132) · [`UpdateAsync_PersistsStatusChange`](LawyerApp.Tests/Integration/Repositories/LegalProcessRepositoryTests.cs#L170) |

---

## V4.1.1 / V4.1.4 – API e Serviços Web

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V4.1.1** | Respostas HTTP incluem Content-Type correcto com charset | Coberto | [`V4_1_1_Register_Response_HasJsonContentType`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L187) · [`V4_1_1_Login_ErrorResponse_HasJsonContentType`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L198) |
| V4.1.4 | Apenas métodos HTTP suportados são aceites | Parcial | [`V14_2_1_LoginEndpoint_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L55) · [`V14_2_1_RegisterEndpoint_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L65) · [`V14_2_1_ClientCreate_IsPost_SoDataNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L74) — verifica rejeição de GET; TRACE coberto por [`V13_4_4_HttpTrace_IsNotSupported`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L85) |

---

## V5.1.1 / V5.2.2 / V5.3.2 – Gestão de Ficheiros

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V5.1.1** | Nome armazenado único por upload; nome original guardado separadamente | Coberto | [`V5_1_1_TwoUploads_WithSameOriginalName_GetDifferentStoredNames`](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L86) · [`V5_1_1_PersistedDocument_StoredFilename_IsUnchangedAfterRetrieval`](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L96) · [`Document_Constructor_GeneratesUniqueStoredFileName`](LawyerApp.Tests/Unit/Domain/ClientTests.cs#L70) |
| **V5.2.2** | Extensão do ficheiro validada; extensão original preservada no nome armazenado | Coberto | [`V5_2_2_StoredFileName_PreservesOriginalExtension` *(4 variantes: .pdf, .docx, .jpg, .txt)*](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L75) · [`Document_StoredFileName_PreservesFileExtension`](LawyerApp.Tests/Unit/Domain/ClientTests.cs#L85) |
| **V5.3.2** | Nome armazenado gerado internamente (prevenção de path traversal / LFI / SSRF) | Coberto | [`V5_3_2_Document_StoredFileName_IsNotTheSameAsUserProvidedName`](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L33) · [`V5_3_2_StoredFileName_ContainsGuid_PreventsEnumeration`](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L43) · [`V5_3_2_PathTraversalInFileName_DoesNotAffectStoredFileName` *(4 variantes)*](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L58) · [`V5_3_2_Document_OriginalFileName_IsStoredSeparately_ForAudit`](LawyerApp.Tests/Unit/Security/ASVS/V5_FileHandlingTests.cs#L112) |

---

## V6.2.8 / V6.3.2 / V6.3.8 – Autenticação

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V6.2.8** | Palavra-passe verificada exactamente como recebida; sem truncagem ou transformação de capitalização | Coberto | [`V6_2_8_Login_PasswordIsVerifiedExactly_CaseSensitive`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L45) · [`V6_2_8_Password_IsPassedToHasher_WithoutModification`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L62) · [`V6_2_8_BCrypt_VerifiesPassword_ExactlyAsProvided`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L141) · [`V6_2_8_BCrypt_RejectsEmptyPassword_AgainstNonEmptyHash`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L152) |
| **V6.3.2** | Contas padrão (admin, root, sa, test) não estão presentes | Coberto | [`V6_3_2_DefaultAccountEmails_AreNotPresent` *(4 variantes)*](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L127) |
| **V6.3.8** | Utilizadores válidos não podem ser deduzidos de falhas de autenticação (enumeração) | Coberto | [`V6_3_8_NonExistentUser_And_WrongPassword_ReturnSameErrorCode`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L82) · [`V6_3_8_ErrorMessage_DoesNotReveal_WhetherUserExists`](LawyerApp.Tests/Unit/Security/ASVS/V6_AuthenticationTests.cs#L106) · [`Login_NonExistentEmail_ReturnsSameStatusAsWrongPassword`](LawyerApp.Tests/Integration/Security/AuthorizationSecurityTests.cs#L107) |

---

## V7.2.1 / V7.2.2 – Gestão de Sessão

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V7.2.1** | Verificação de token de sessão efectuada pelo serviço backend | Coberto | [`V7_2_1_Token_ContainsSubjectClaim_ForBackendVerification`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L155) · [`Login_WhenUserDoesNotExist_ReturnsFailureWith401`](LawyerApp.Tests/Unit/Services/LoginServiceTests.cs#L38) · [`Login_WhenPasswordIsWrong_ReturnsFailureWith401`](LawyerApp.Tests/Unit/Services/LoginServiceTests.cs#L51) |
| **V7.2.2** | Tokens gerados dinamicamente; sem API keys estáticas | Coberto | [`V7_2_2_EachToken_IsUnique_NotStatic`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L145) · [`Generate_TwoDifferentUsers_ProduceDifferentTokens`](LawyerApp.Tests/Unit/Security/JwtProviderTests.cs#L149) |

---

## V8.2.1 / V8.2.2 / V8.3.1 – Autorização

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V8.2.1** | Acesso ao nível de função restrito a utilizadores com permissões explícitas; tokens inválidos rejeitados | Coberto | [`V8_2_1_RequestWithNoToken_ToProtectedConcept_IsRejected`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L65) · [`V8_2_1_MalformedToken_FailsValidation`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L97) · [`V8_2_1_ExpiredToken_FailsValidation`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L109) · [`V8_2_1_TokenSignedWithWrongKey_FailsValidation`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L122) |
| **V8.2.2** | Acesso a dados específicos restrito (prevenção IDOR / BOLA) | Coberto | [`V2_2_1_UserHasAccess_IsEnforced_ByProcessRelation`](LawyerApp.Tests/Unit/Security/ASVS/V2_InputValidationTests.cs#L152) · [`UserHasAccessToProcessAsync_WhenUserIsLawyer_ReturnsTrue`](LawyerApp.Tests/Integration/Repositories/LegalProcessRepositoryTests.cs#L125) · [`UserHasAccessToProcessAsync_WhenUserIsClient_ReturnsTrue`](LawyerApp.Tests/Integration/Repositories/LegalProcessRepositoryTests.cs#L137) · [`UserHasAccessToProcessAsync_WhenUserHasNoRelation_ReturnsFalse`](LawyerApp.Tests/Integration/Repositories/LegalProcessRepositoryTests.cs#L149) · [`UserHasAccessToProcessAsync_WhenProcessDoesNotExist_ReturnsFalse`](LawyerApp.Tests/Integration/Repositories/LegalProcessRepositoryTests.cs#L160) |
| **V8.3.1** | Autorização aplicada no backend; não depende de controlos do lado do cliente | Coberto | [`V8_3_1_AuthorizationEnforced_AtBackend_RejectsUnauthenticated`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L80) |

---

## V9.1.1 / V9.1.2 / V9.1.3 / V9.2.1 / V9.2.3 – Tokens Auto-contidos

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V9.1.1** | Tokens validados com assinatura digital / MAC antes de aceitar conteúdo | Coberto | [`V9_1_1_ValidToken_PassesSignatureValidation`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L65) · [`V9_1_1_TamperedToken_FailsSignatureValidation`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L76) · [`V9_1_1_TokenSignedWithDifferentKey_IsRejected`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L93) |
| **V9.1.2** | Apenas algoritmos da lista permitida; algoritmo `none` rejeitado | Coberto | [`V9_1_2_HmacSha256_IsAcceptedAlgorithm`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L108) · [`V9_1_2_NoneAlgorithm_IsRejectedByValidationParameters`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L119) |
| **V9.1.3** | Material de chave provém de fontes pré-configuradas e confiáveis | Coberto | [`V9_1_3_TokenWithWrongIssuer_IsRejected`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L208) · [`V9_1_3_TokenWithCorrectIssuer_IsAccepted`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L220) · [`V9_1_1_TokenSignedWithDifferentKey_IsRejected`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L93) |
| **V9.2.1** | Claim `exp` presente e verificada; token expirado rejeitado | Coberto | [`V9_2_1_Token_ContainsExpClaim`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L139) · [`V9_2_1_ExpiredToken_IsRejectedByValidator`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L148) · [`V9_2_1_TokenWithFutureExp_IsAccepted`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L160) · [`Generate_TokenIsNotExpiredImmediately`](LawyerApp.Tests/Unit/Security/JwtProviderTests.cs#L115) · [`Generate_TokenExpiresInApproximately30Minutes`](LawyerApp.Tests/Unit/Security/JwtProviderTests.cs#L124) · [`V8_2_1_ExpiredToken_FailsValidation`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L109) |
| **V9.2.3** | Claim `aud` validada contra lista permitida | Coberto | [`V9_2_3_Token_ContainsAudienceClaim`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L173) · [`V9_2_3_TokenWithWrongAudience_IsRejected`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L183) · [`V9_2_3_TokenWithCorrectAudience_IsAccepted`](LawyerApp.Tests/Unit/Security/ASVS/V9_SelfContainedTokenTests.cs#L195) |

---

## V11.2.3 / V11.3.1 / V11.4.1 / V11.4.2 – Criptografia

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V11.2.3** | Todas as primitivas criptográficas atingem mínimo de 128 bits de segurança | Coberto | [`V11_2_3_JwtSigningKey_HasSufficientLength`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L120) · [`V11_2_3_Sha256_Provides256BitOutput_MeetsMinimumSecurity`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L132) |
| **V11.3.1** / **V11.3.2** | Modos inseguros (ECB) e preenchimento fraco (PKCS#1 v1.5) não utilizados; AES-GCM disponível | Coberto | [`V11_3_1_AesGcm_IsAvailable_And_Provides_AuthenticatedEncryption`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L146) |
| **V11.4.1** | Apenas funções de hash aprovadas (SHA-256+); MD5 e SHA-1 não utilizados | Coberto | [`V11_4_1_Sha256_IsApprovedHashFunction`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L82) · [`V11_4_1_Md5_OutputLength_RevealsThatItIsNotApproved`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L92) · [`V11_4_1_Sha1_OutputLength_RevealsThatItIsNotApproved`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L106) |
| **V11.4.2** | Palavras-passe armazenadas com KDF computacionalmente intensivo (bcrypt) com parâmetros adequados | Coberto | [`V11_4_2_PasswordHash_UsesBCryptFormat`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L24) · [`V11_4_2_BCryptHash_ContainsWorkFactor`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L34) · [`V11_4_2_PasswordVerification_WorksCorrectlyWithBCrypt`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L49) · [`V11_4_2_DifferentPasswords_ProduceDifferentHashes`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L59) · [`V11_4_2_SamePassword_ProducesDifferentHashesOnEachCall`](LawyerApp.Tests/Unit/Security/ASVS/V11_CryptographyTests.cs#L69) · [`HashPassword_ProducesBCryptFormatHash`](LawyerApp.Tests/Unit/Security/BCryptPasswordHasherTests.cs#L64) · [`HashPassword_ReturnsNonEmptyHash`](LawyerApp.Tests/Unit/Security/BCryptPasswordHasherTests.cs#L12) · [`HashPassword_TwoCallsProduceDifferentHashes`](LawyerApp.Tests/Unit/Security/BCryptPasswordHasherTests.cs#L29) |

---

## V13.4.2 / V13.4.4 – Configuração

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V13.4.4** | Método HTTP TRACE desactivado | Coberto | [`V13_4_4_HttpTrace_IsNotSupported`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L85) |
| **V13.4.2** | Modo de debug desactivado; informação interna não exposta em erros | Coberto | [`V13_4_2_ErrorResponse_DoesNotContainStackTrace`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L97) |

---

## V14.2.1 – Protecção de Dados

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V14.2.1** | Dados sensíveis enviados apenas no corpo do pedido; nunca em URLs ou query strings | Coberto | [`V14_2_1_LoginEndpoint_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L55) · [`V14_2_1_RegisterEndpoint_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L65) · [`V14_2_1_ClientCreate_IsPost_SoDataNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L74) · [`V14_2_1_Login_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L167) · [`V14_2_1_Register_IsPost_SoCredentialsNeverInUrl`](LawyerApp.Tests/Integration/Security/ASVS/V8_AuthorizationTests.cs#L176) · [`V14_2_1_RegisterResponse_DoesNotContainPassword`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L139) · [`V14_2_1_ClientCreate_ResponseDoesNotContainPassword`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L152) · [`V14_2_1_JwtPayload_DoesNotContainPassword`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L167) · [`Create_ResponseDoesNotContainPasswordHash`](LawyerApp.Tests/Integration/API/ClientControllerTests.cs#L35) · [`Register_ResponseDoesNotContainPasswordHash`](LawyerApp.Tests/Integration/API/LoginControllerTests.cs#L41) · [`Login_ResponseNeverContainsPasswordHash`](LawyerApp.Tests/Integration/API/LoginControllerTests.cs#L75) · [`GetAllClientsAsync_DoesNotExposePasswordHash`](LawyerApp.Tests/Unit/Services/ClientServiceTests.cs#L155) |

---

## V16 – Logging e Tratamento de Erros

| Req ID | Descrição | Estado | Testes |
|---|---|---|---|
| **V16** | Respostas de erro não expõem stack traces, caminhos internos ou nomes de excepção | Coberto | [`V16_ErrorResponse_DoesNotLeakInternalPaths`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L110) · [`V16_RegisterError_DoesNotLeakInternalDetails`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L122) · [`V13_4_2_ErrorResponse_DoesNotContainStackTrace`](LawyerApp.Tests/Integration/Security/ASVS/V14_DataProtectionTests.cs#L97) · [`Login_ErrorResponse_DoesNotLeakPasswordHash`](LawyerApp.Tests/Integration/Security/AuthorizationSecurityTests.cs#L119) |

---

## Resumo de Cobertura

| Capítulo ASVS | Requisitos com Testes 
|---|---|
| V1 – Codificação e Sanitização | 1 
| V2 – Validação e Lógica de Negócio | 3 
| V4 – API e Serviços Web | 2 
| V5 – Gestão de Ficheiros | 3 
| V6 – Autenticação | 3 
| V7 – Gestão de Sessão | 2 
| V8 – Autorização | 3 | |
| V9 – Tokens Auto-contidos | 5 
| V11 – Criptografia | 4 
| V13 – Configuração | 2 
| V14 – Protecção de Dados | 1 
| V16 – Logging e Erros | 1 
| **Total** | **30** 
