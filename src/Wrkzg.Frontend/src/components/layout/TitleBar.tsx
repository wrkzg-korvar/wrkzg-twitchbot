import { usePlatform } from "../../hooks/usePlatform";
import { MacTitleBar } from "./MacTitleBar";

export function TitleBar() {
  const platform = usePlatform();

  // Windows uses native title bar from Photino (not chromeless)
  if (platform !== "macos") {
    return null;
  }

  return <MacTitleBar />;
}
