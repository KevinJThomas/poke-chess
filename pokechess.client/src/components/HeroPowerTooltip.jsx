import { Popover, ArrowContainer } from "react-tiny-popover";
import Markdown from "react-markdown";

export default function HeroPowerTooltip({
  children,
  showToolTip,
  positions,
  text,
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
            <Markdown>{text}</Markdown>
          </div>
        </ArrowContainer>
      )}
    >
      {children}
    </Popover>
  );
}
