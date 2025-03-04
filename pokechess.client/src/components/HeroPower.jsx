import Button from "./Button";
import HeroPowerTooltip from "./HeroPowerTooltip";
import { useState } from "react";

export default function HeroPower({
  cost,
  text,
  isDisabled,
  isPassive,
  connection,
  name,
}) {
  const [showToolTip, setShowToolTip] = useState();

  function onClick() {
    if (isPassive) {
      return;
    }

    connection.invoke("HeroPower");
  }

  return (
    <HeroPowerTooltip showToolTip={showToolTip} text={text}>
      <div
        onMouseEnter={() => setShowToolTip(true)}
        onMouseLeave={() => setShowToolTip(false)}
      >
        <Button
          disabled={isDisabled}
          className="h-20 w-20 rounded-full"
          onClick={onClick}
        >
          <span className="text-xs">
            {name} ({cost})
          </span>
        </Button>
      </div>
    </HeroPowerTooltip>
  );
}
