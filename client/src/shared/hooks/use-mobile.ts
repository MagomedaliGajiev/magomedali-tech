import * as React from "react"

const MOBILE_BREAKPOINT = 768

const mobileQuery = `(max-width: ${MOBILE_BREAKPOINT - 1}px)`

export function useIsMobile() {
  const [isMobile, setIsMobile] = React.useState(false)

  React.useEffect(() => {
    const mediaQuery = window.matchMedia(mobileQuery)
    const updateMobileState = () => setIsMobile(mediaQuery.matches)
    const initialUpdate = window.setTimeout(updateMobileState, 0)

    mediaQuery.addEventListener("change", updateMobileState)

    return () => {
      window.clearTimeout(initialUpdate)
      mediaQuery.removeEventListener("change", updateMobileState)
    }
  }, [])

  return isMobile
}
