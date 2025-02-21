import Button from "./Button";
import HeroPowerTooltip from "./HeroPowerTooltip";
import { useState } from "react";

export default function HeroPower({ cost, text, isDisabled }) {
  const [showToolTip, setShowToolTip] = useState();
  return (
    <HeroPowerTooltip showToolTip={showToolTip} text={text}>
      <div
        onMouseEnter={() => setShowToolTip(true)}
        onMouseLeave={() => setShowToolTip(false)}
      >
        <Button disabled={isDisabled} className="h-16 w-16 rounded-full">
          hi ({cost})
        </Button>
      </div>
    </HeroPowerTooltip>
  );
}
