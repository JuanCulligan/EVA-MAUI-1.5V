-- Crear en la base DB_EVA_ (ajusta el nombre de la tabla y columnas para que coincidan
-- con la tabla que usa dbo.SP_InsertarViajeGuardado).
IF OBJECT_ID(N'dbo.SP_EliminarViajeGuardado', N'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_EliminarViajeGuardado;
GO

CREATE PROCEDURE dbo.SP_EliminarViajeGuardado
    @GUID_USUARIO UNIQUEIDENTIFIER,
    @NOMBRE_VIAJE NVARCHAR(100),
    @LATITUD_DESTINO DECIMAL(10, 6),
    @LONGITUD_DESTINO DECIMAL(10, 6)
AS
BEGIN
    SET NOCOUNT ON;

    -- TODO: Reemplaza [TU_TABLA_VIAJES_GUARDADOS] y nombres de columnas por los reales.
    DELETE FROM [dbo].[TU_TABLA_VIAJES_GUARDADOS]
    WHERE [GUID_USUARIO] = @GUID_USUARIO
      AND [NOMBRE_VIAJE] = @NOMBRE_VIAJE
      AND [LATITUD_DESTINO] = @LATITUD_DESTINO
      AND [LONGITUD_DESTINO] = @LONGITUD_DESTINO;

    IF @@ROWCOUNT > 0
        SELECT 1 AS Resultado, N'OK' AS Mensaje;
    ELSE
        SELECT 0 AS Resultado, N'No se encontró el favorito' AS Mensaje;
END
GO
