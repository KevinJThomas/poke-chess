import { Popover, ArrowContainer } from "react-tiny-popover";
import Markdown from "react-markdown";

export default function Tooltip({ children, showToolTip, text }) {
  return (
    <Popover
      isOpen={showToolTip}
      positions={["top", "bottom", "left", "right"]} // preferred positions by priority
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
          <div className="rounded-md bg-gray-800 p-2 text-sm text-white">
            <Markdown>{text}</Markdown>
          </div>
        </ArrowContainer>
      )}
    >
      {children}
    </Popover>
  );
}
