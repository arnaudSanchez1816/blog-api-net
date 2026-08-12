export interface ParsedErrorResponse {
    errorMessage: string
    status: number
    errors?: Record<string, string>[]
}

export async function parseErrorResponse(
    errorResponse: Response
): Promise<ParsedErrorResponse> {
    const { status, statusText } = errorResponse
    let parsedError: ParsedErrorResponse = {
        status: status,
        errorMessage: statusText,
    }
    if (errorResponse.body) {
        try {
            const errorBody = (await errorResponse.json()) as {
                errors: Record<string, string>[]
                title: string
                detail: string
            }
            parsedError = {
                ...parsedError,
                errorMessage: errorBody.title ?? statusText,
                errors: errorBody.errors,
            }
        } catch (e) {
            if (e instanceof SyntaxError == false) {
                console.error(e)
            }
        }
    }

    return parsedError
}
