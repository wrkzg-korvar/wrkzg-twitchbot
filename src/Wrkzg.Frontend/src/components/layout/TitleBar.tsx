import { usePlatform } from "../../hooks/usePlatform";
import { MacTitleBar } from "./MacTitleBar";
import { WinTitleBar } from "./WinTitleBar";

export function TitleBar() {
  const platform = usePlatform();

  if (platform === "macos") {
    return <MacTitleBar />;
  }

  return <WinTitleBar />;
}
