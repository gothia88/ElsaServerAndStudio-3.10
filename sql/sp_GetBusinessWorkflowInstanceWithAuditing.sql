CREATE OR ALTER PROCEDURE [Elsa].[sp_GetBusinessWorkflowInstanceWithAuditing]
    @Id NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Q AS
    (
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
    )

    SELECT
        Q.Id,
        Q.WorkflowDefinitionId,
        Q.WorkflowDefinitionVersionId,
        Q.WorkflowInstanceId,
        Q.WorkflowVersion,
        Q.ActivityInstanceId,
        Q.ParentActivityInstanceId,
        Q.ActivityId,
        Q.ActivityType,
        Q.ActivityTypeVersion,
        Q.ActivityName,
        Q.ActivityNodeId,
        Q.Timestamp,
        Q.Sequence,
        Q.EventName,
        Q.Message,
        Q.Source,
        Q.SerializedActivityState,
        Q.SerializedPayload,
        Q.TenantId,

        LAST_VALUE(DisplayText) IGNORE NULLS OVER
        (
            ORDER BY Sequence
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS DisplayText,

        LAST_VALUE(Auditing) IGNORE NULLS OVER
        (
            ORDER BY Sequence
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS Auditing

    FROM Q
    ORDER BY Sequence;
END;
