namespace Application.Exceptions;

public static class Errors
{
    public static readonly string InvalidContactId = "REG003";
    public static readonly string InvalidContactIdMessage = "The contactId {contactId} is invalid";

    public static readonly string NotFoundOperationCode = "REG001";
    public static readonly string NotFoundOperationMessage = "L'opération avec l'identifiant {0} est introuvable";

    public static readonly string BadRequestOperationPatchCode = "REG002";
    public static readonly string BadRequestOperationPatchMessage = "Impossible de mettre à jour l'opération : les informations fournies dans la requête sont incorrectes.";

    public static readonly string InvalidSiretCode = "REG004";

}
