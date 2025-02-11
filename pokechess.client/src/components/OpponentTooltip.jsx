import { Popover, ArrowContainer } from "react-tiny-popover";
import { cn } from "../util";

export default function OpponentTooltip({
  children,
  showToolTip,
  combatHistory,
  winStreak,
}) {
  if (combatHistory.length === 0) {
    return children;
  }

  return (
    <Popover
      containerStyle={{ zIndex: 10 }}
      isOpen={showToolTip}
      positions={["right"]} // preferred positions by priority
      content={({ position, childRect, popoverRect }) => (
        <ArrowContainer // if you'd like an arrow, you can import the ArrowContainer!
          position={position}
          childRect={childRect}
          popoverRect={popoverRect}
          arrowColor="#1e2939"
          padding={2}
          arrowSize={10}
          arrowStyle={{}}
          className="popover-arrow-container"
          arrowClassName="popover-arrow"
        >
          <div className="flex flex-col items-center gap-2 rounded-md bg-gray-800 p-2 text-sm text-white">
            <div className="flex h-6 w-6 items-center justify-center rounded-full bg-emerald-700 font-bold text-white">
              {winStreak}
            </div>
            {combatHistory.map((combat, index) => (
              <div
                className="flex w-24 items-center justify-between gap-4 truncate rounded-sm bg-red-300 p-1"
                key={index}
              >
                {combat.damage && (
                  <span
                    className={cn(
                      "font-bold",
                      combat.damage < 0 &&
                        "rounded-sm bg-red-800 px-1 text-white",
                      combat.damage > 0 &&
                        "rounded-sm bg-emerald-700 px-1 text-white",
                    )}
                  >
                    {combat.damage > 0 && <span>+</span>}
                    {combat.damage}
                  </span>
                )}
                <span className="text-black">{combat.name}</span>{" "}
              </div>
            ))}
          </div>
        </ArrowContainer>
      )}
    >
      {children}
    </Popover>
  );
}
