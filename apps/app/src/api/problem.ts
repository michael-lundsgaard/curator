// The API's own words for a failure it expected (a 4xx ProblemDetails: "A collection can't be moved into itself…"), or
// the fallback for anything else: a server error, or no response at all.
export function problemMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null && "status" in error && "detail" in error) {
    const { status, detail } = error;
    if (typeof status === "number" && status < 500 && typeof detail === "string") return detail;
  }
  return fallback;
}
