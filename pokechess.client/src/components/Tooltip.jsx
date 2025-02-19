import { Popover, ArrowContainer } from "react-tiny-popover";
import Markdown from "react-markdown";
import TypeTooltip from "./TypeTooltip";
import Tier from "./Tier";

export default function Tooltip({
  children,
  showToolTip,
  text,
  types,
  positions,
  location,
  tier,
  name,
  cardType,
}) {
  return (
    <Popover
      containerStyle={{ zIndex: 10 }}
      isOpen={showToolTip}
      positions={positions} // preferred positions by priority
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
          <div className="flex max-w-96 flex-col items-center gap-2 rounded-md bg-gray-800 p-2 text-center text-sm text-white">
            {cardType === 0 && <p className="font-bold italic">{name}</p>}
            <Markdown>{text}</Markdown>
            <div className="flex w-full flex-row items-center justify-between gap-3">
              <div className="w-6"></div>
              <div>{!!types.length && <TypeTooltip types={types} />}</div>
              <div className="w-6">
                {location !== "shop" && !!tier && (
                  <Tier tier={tier} className="relative" />
                )}
              </div>
            </div>
          </div>
        </ArrowContainer>
      )}
    >
      {children}
    </Popover>
  );
}
