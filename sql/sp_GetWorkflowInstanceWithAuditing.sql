CREATE OR ALTER PROCEDURE [Elsa].[sp_GetWorkflowInstanceWithAuditing]
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        WI.*,
        A.DisplayText,
        A.Description AS Auditing
    FROM
    (
        SELECT L.*
        FROM
        (
            SELECT *
            FROM dbo.ActivityExecutionRecords
            WHERE WorkflowInstanceId = @Id

            UNION

            SELECT *
            FROM Elsa.ActivityExecutionRecords
            WHERE WorkflowInstanceId = @Id
        ) R
        INNER JOIN
        (
            SELECT *
            FROM dbo.WorkflowExecutionLogRecords
            WHERE WorkflowInstanceId = @Id

            UNION

            SELECT *
            FROM Elsa.WorkflowExecutionLogRecords
            WHERE WorkflowInstanceId = @Id
        ) L
            ON R.Id = L.ActivityInstanceId
    ) WI

    INNER JOIN Elsa.WorkflowInstances WFI
        ON WFI.Id = WI.WorkflowInstanceId

    INNER JOIN Elsa.WorkflowDefinitions WD
        ON WD.DefinitionId = WFI.DefinitionId
       AND WD.Version = WFI.Version

    OUTER APPLY
    (
        SELECT TOP (1)
            J.DisplayText,
            J.Description
        FROM OPENJSON(WD.StringData, '$.activities')
        WITH
        (
            ActivityId  NVARCHAR(100) '$.id',
            DisplayText NVARCHAR(500) '$.metadata.displayText',
            Description NVARCHAR(500) '$.metadata.description'
        ) J
        WHERE J.ActivityId = WI.ActivityId
    ) A

    ORDER BY WI.Sequence;
END;
